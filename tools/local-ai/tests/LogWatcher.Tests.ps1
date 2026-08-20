BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
    function New-WatcherRepo([string]$Path) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Set-Content (Join-Path $Path 'README.md') '# test'
        git -C $Path init --quiet; git -C $Path config user.email 'tests@example.invalid'; git -C $Path config user.name 'Bridge Tests'
        git -C $Path add README.md; git -C $Path commit --quiet -m baseline
        return $Path
    }
    function Wait-ForCondition([scriptblock]$Condition,[int]$Milliseconds=5000) {
        $until=[DateTime]::UtcNow.AddMilliseconds($Milliseconds)
        do { if(& $Condition){return $true}; Start-Sleep -Milliseconds 100 } while([DateTime]::UtcNow -lt $until)
        return $false
    }
}

Describe 'Development-session log watcher' {
    BeforeEach {
        $script:Repo=New-WatcherRepo (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $script:Task=New-LocalAiTask -Goal 'Watch log' -Mode implementation -RepositoryRoot $script:Repo
        $script:Game=Join-Path $TestDrive ('game-'+[guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path (Join-Path $script:Game 'BepInEx') -Force | Out-Null
        $script:Log=Join-Path $script:Game 'BepInEx\LogOutput.log'
        Set-Content $script:Log 'OLD SCRC LINE'
    }
    AfterEach {
        Stop-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ErrorAction SilentlyContinue | Out-Null
    }

    It 'tails from end and captures only matching appended lines' {
        $session=Start-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -GameDir $script:Game -Pattern @('SCRC') -TimeoutMinutes 1
        Add-Content $script:Log 'noise'
        Add-Content $script:Log 'NEW SCRC LINE'
        Wait-ForCondition { (Test-Path $session.CapturePath) -and ((Get-Content -Raw $session.CapturePath) -match 'NEW SCRC LINE') } | Should -BeTrue
        $capture=Get-Content -Raw $session.CapturePath
        $capture | Should -Match 'NEW SCRC LINE'
        $capture | Should -Not -Match 'OLD SCRC LINE|noise'
        $session.OwningProcessId | Should -Be $PID
    }

    It 'rejects duplicate active sessions' {
        Start-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -GameDir $script:Game | Out-Null
        { Start-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -GameDir $script:Game } |
            Should -Throw '*already active*'
    }

    It 'stops explicitly and is idempotent' {
        Start-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -GameDir $script:Game | Out-Null
        (Stop-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo).Stopped | Should -BeTrue
        (Stop-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo).Stopped | Should -BeTrue
    }

    It 'stops when the timeout expires' {
        $session=Start-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -GameDir $script:Game -TimeoutMinutes 0.01
        Wait-ForCondition { (Get-Job -Id $session.JobId).State -ne 'Running' } | Should -BeTrue
    }

    It 'stops when the task becomes terminal' {
        $session=Start-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -GameDir $script:Game
        Set-LocalAiTaskState -Task (Get-LocalAiTask -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo) -State failed | Out-Null
        Wait-ForCondition { (Get-Job -Id $session.JobId).State -ne 'Running' } | Should -BeTrue
    }

    It 'requires the exact existing game log' {
        Remove-Item $script:Log
        { Start-LocalAiDevelopmentSession -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -GameDir $script:Game } |
            Should -Throw '*LogOutput.log*does not exist*'
    }

    It 'rejects investigation tasks' {
        $read=New-LocalAiTask -Goal 'Read' -Mode investigation -RepositoryRoot $script:Repo
        { Start-LocalAiDevelopmentSession -TaskId $read.TaskId -RepositoryRoot $script:Repo -GameDir $script:Game } |
            Should -Throw '*not allowed for investigation*'
    }
}
