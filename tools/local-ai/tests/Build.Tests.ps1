BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
    function New-BuildTestRepository {
        param([string] $Path)
        New-Item -ItemType Directory -Path (Join-Path $Path 'tools') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $Path 'client') -Force | Out-Null
        Set-Content (Join-Path $Path 'README.md') '# test'
        Set-Content (Join-Path $Path 'tools\validate-repo.py') 'print("ok")'
        Set-Content (Join-Path $Path 'tools\build-apworld.ps1') 'Write-Output ok'
        Set-Content (Join-Path $Path 'client\build.ps1') 'param([string]$GameDir,[switch]$SkipInstall)'
        git -C $Path init --quiet
        git -C $Path config user.email 'tests@example.invalid'
        git -C $Path config user.name 'Bridge Tests'
        git -C $Path add .
        git -C $Path commit --quiet -m baseline
        return $Path
    }
}

Describe 'Named bridge build operations' {
    BeforeEach {
        $script:Repo = New-BuildTestRepository (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $script:WtRoot = Join-Path $TestDrive ('wt-' + [guid]::NewGuid().ToString('N'))
        $script:Config = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '.psd1')
        Set-Content $script:Config "@{ WorktreeRoot = '$($script:WtRoot.Replace("'","''"))' }"
        $script:Task = New-LocalAiTask -Goal 'Build test' -Mode implementation -RepositoryRoot $script:Repo -ConfigPath $script:Config
        $script:Wt = New-LocalAiWorktree -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config
    }
    AfterEach {
        if ($script:Wt -and (Test-Path $script:Wt.Path)) { git -C $script:Repo worktree remove --force $script:Wt.Path 2>$null }
        git -C $script:Repo worktree prune 2>$null
    }

    It 'runs only the repository validator from the task worktree' {
        InModuleScope LocalAiBridge -Parameters @{ Repo=$script:Repo; Config=$script:Config; TaskId=$script:Task.TaskId; Wt=$script:Wt.Path } {
            Mock Invoke-BridgeProcess { [pscustomobject]@{ ExitCode=0; Output=@('Repository validation passed.') } }
            $result = Invoke-LocalAiValidation -TaskId $TaskId -RepositoryRoot $Repo -ConfigPath $Config
            $result.Succeeded | Should -BeTrue
            Should -Invoke Invoke-BridgeProcess -Times 1 -ParameterFilter {
                $FilePath -eq 'py' -and $ArgumentList[0] -eq '-3' -and
                $ArgumentList[1] -eq (Join-Path $Wt 'tools\validate-repo.py') -and $WorkingDirectory -eq $Wt
            }
        }
    }

    It 'builds the client without deployment' {
        InModuleScope LocalAiBridge -Parameters @{ Repo=$script:Repo; Config=$script:Config; TaskId=$script:Task.TaskId; Wt=$script:Wt.Path } {
            Mock Invoke-BridgeProcess { [pscustomobject]@{ ExitCode=0; Output=@('built') } }
            $result = Invoke-LocalAiBuild -TaskId $TaskId -RepositoryRoot $Repo -ConfigPath $Config -Component client -GameDir 'C:\Games\Titus'
            $result.Succeeded | Should -BeTrue
            Should -Invoke Invoke-BridgeProcess -Times 1 -ParameterFilter {
                $FilePath -eq 'pwsh' -and $ArgumentList -contains '-SkipInstall' -and
                $ArgumentList -contains 'C:\Games\Titus' -and $WorkingDirectory -eq $Wt
            }
        }
    }

    It 'persists nonzero operation results' {
        InModuleScope LocalAiBridge -Parameters @{ Repo=$script:Repo; Config=$script:Config; TaskId=$script:Task.TaskId } {
            Mock Invoke-BridgeProcess { [pscustomobject]@{ ExitCode=9; Output=@('failed') } }
            { Invoke-LocalAiValidation -TaskId $TaskId -RepositoryRoot $Repo -ConfigPath $Config } | Should -Throw '*exit code 9*'
            $task = Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $Repo -ConfigPath $Config
            $task.Operations[-1].ExitCode | Should -Be 9
            $task.Operations[-1].Succeeded | Should -BeFalse
        }
    }

    It 'rejects investigation tasks before process execution' {
        $readTask = New-LocalAiTask -Goal 'Read' -Mode investigation -RepositoryRoot $script:Repo -ConfigPath $script:Config
        { Invoke-LocalAiValidation -TaskId $readTask.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config } |
            Should -Throw '*not allowed for investigation*'
    }
}

Describe 'Client build script non-deploy mode' {
    It 'supports SkipInstall without creating the plugin directory' {
        $sandbox = Join-Path $TestDrive 'client'
        New-Item -ItemType Directory -Path $sandbox -Force | Out-Null
        Copy-Item (Join-Path $PSScriptRoot '..\..\..\client\build.ps1') (Join-Path $sandbox 'build.ps1')
        Set-Content (Join-Path $sandbox 'RhythmCastleAP.csproj') '<Project />'
        $game = Join-Path $TestDrive 'game'
        New-Item -ItemType Directory -Path $game -Force | Out-Null
        function global:dotnet {
            $out = Join-Path $sandbox 'bin\Release\net6.0'
            New-Item -ItemType Directory -Path $out -Force | Out-Null
            Set-Content (Join-Path $out 'RhythmCastleAP.dll') 'fake'
        }
        try { & (Join-Path $sandbox 'build.ps1') -GameDir $game -SkipInstall }
        finally { Remove-Item Function:\dotnet -ErrorAction SilentlyContinue }
        Test-Path (Join-Path $game 'BepInEx\plugins\RhythmCastleAP') | Should -BeFalse
    }
}
