BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
    function New-DeploymentRepository([string]$Path) {
        New-Item -ItemType Directory -Path (Join-Path $Path 'client') -Force | Out-Null
        Set-Content (Join-Path $Path 'README.md') '# test'
        git -C $Path init --quiet; git -C $Path config user.email 'tests@example.invalid'; git -C $Path config user.name 'Bridge Tests'
        git -C $Path add .; git -C $Path commit --quiet -m baseline
        return $Path
    }
    function Add-SuccessfulClientBuild {
        $task = Get-LocalAiTask -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config
        $task | Add-Member -NotePropertyName Operations -NotePropertyValue @([pscustomobject]@{ Operation='build'; Component='client'; ExitCode=0; Succeeded=$true }) -Force
        $task | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $task.TaskDirectory 'task.json')
        $out = Join-Path $script:Wt.Path 'client\bin\Release\net6.0'
        New-Item -ItemType Directory -Path $out -Force | Out-Null
        Set-Content (Join-Path $out 'RhythmCastleAP.dll') 'plugin'
        Set-Content (Join-Path $out 'Archipelago.MultiClient.Net.dll') 'dependency'
        Set-Content (Join-Path $out 'BepInEx.Core.dll') 'exclude'
        Set-Content (Join-Path $out '0Harmony.dll') 'exclude'
        Set-Content (Join-Path $out 'Il2CppInterop.Runtime.dll') 'exclude'
    }
}

Describe 'Restricted client deployment' {
    BeforeEach {
        $script:Repo = New-DeploymentRepository (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $script:WtRoot = Join-Path $TestDrive ('wt-' + [guid]::NewGuid().ToString('N'))
        $script:Config = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '.psd1')
        Set-Content $script:Config "@{ WorktreeRoot = '$($script:WtRoot.Replace("'","''"))' }"
        $script:Task = New-LocalAiTask -Goal 'Deploy test' -Mode implementation -RepositoryRoot $script:Repo -ConfigPath $script:Config
        $script:Wt = New-LocalAiWorktree -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config
        $script:Game = Join-Path $TestDrive ('game-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Game -Force | Out-Null
    }
    AfterEach {
        if ($script:Wt -and (Test-Path $script:Wt.Path)) { git -C $script:Repo worktree remove --force $script:Wt.Path 2>$null }
        git -C $script:Repo worktree prune 2>$null
    }

    It 'requires explicit deployment confirmation' {
        { Publish-LocalAiClient -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config -GameDir $script:Game } |
            Should -Throw '*ConfirmDeployment*'
    }

    It 'requires a successful client build record' {
        { Publish-LocalAiClient -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config -GameDir $script:Game -ConfirmDeployment } |
            Should -Throw '*successful client build*'
    }

    It 'copies only permitted DLLs to the exact plugin directory and records hashes' {
        Add-SuccessfulClientBuild
        $result = Publish-LocalAiClient -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config -GameDir $script:Game -ConfirmDeployment
        $expected = [IO.Path]::GetFullPath((Join-Path $script:Game 'BepInEx\plugins\RhythmCastleAP'))
        $result.Destination | Should -Be $expected
        $result.Files.Name | Should -Be @('Archipelago.MultiClient.Net.dll','RhythmCastleAP.dll')
        Get-ChildItem $expected -File | Select-Object -ExpandProperty Name | Sort-Object | Should -Be @('Archipelago.MultiClient.Net.dll','RhythmCastleAP.dll')
        $result.Files[1].Sha256 | Should -Be (Get-FileHash (Join-Path $expected 'RhythmCastleAP.dll') -Algorithm SHA256).Hash
    }

    It 'rejects a reparse escape in the plugin path' {
        Add-SuccessfulClientBuild
        $outside = Join-Path $TestDrive 'outside-plugins'
        New-Item -ItemType Directory -Path (Join-Path $script:Game 'BepInEx'),$outside -Force | Out-Null
        New-Item -ItemType Junction -Path (Join-Path $script:Game 'BepInEx\plugins') -Target $outside | Out-Null
        { Publish-LocalAiClient -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config -GameDir $script:Game -ConfirmDeployment } |
            Should -Throw '*reparse point*'
        Test-Path (Join-Path $outside 'RhythmCastleAP') | Should -BeFalse
    }
}
