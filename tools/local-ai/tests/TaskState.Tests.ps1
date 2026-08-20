BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force

    function New-TestRepository {
        param([string] $Path)
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        git -C $Path init --quiet
        git -C $Path config user.email 'tests@example.invalid'
        git -C $Path config user.name 'Bridge Tests'
        Set-Content -LiteralPath (Join-Path $Path 'README.md') -Value '# Test'
        git -C $Path add README.md
        git -C $Path commit --quiet -m baseline
        return $Path
    }
}

Describe 'Local AI task state' {
    BeforeEach {
        $env:OPENWEBUI_API_KEY = 'task-test-secret'
        $script:Repo = New-TestRepository -Path (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
    }

    It 'creates a schema-valid task with baseline and safe slug' {
        $task = New-LocalAiTask -Goal 'Investigate Level 4 glasses / Minim progression.' -Mode investigation -RepositoryRoot $script:Repo

        $task.SchemaVersion | Should -Be 1
        $task.TaskId | Should -Match '^[a-f0-9]{12}$'
        $task.Slug | Should -Be 'investigate-level-4-glasses-minim-progression'
        $task.State | Should -Be 'created'
        $task.Mode | Should -Be 'investigation'
        $task.BaselineCommit | Should -Be (git -C $script:Repo rev-parse HEAD)
        $task.CreatedAtUtc | Should -Match 'Z$'
        Test-Path -LiteralPath (Join-Path $task.TaskDirectory 'task.json') | Should -BeTrue
    }

    It 'loads a task by ID' {
        $created = New-LocalAiTask -Goal 'Test load' -Mode implementation -RepositoryRoot $script:Repo
        $loaded = Get-LocalAiTask -TaskId $created.TaskId -RepositoryRoot $script:Repo

        $loaded.TaskId | Should -Be $created.TaskId
        $loaded.Goal | Should -Be 'Test load'
        $loaded.Mode | Should -Be 'implementation'
    }

    It 'permits the approved investigation path to awaiting review' {
        $task = New-LocalAiTask -Goal 'Test transitions' -Mode investigation -RepositoryRoot $script:Repo
        foreach ($state in 'context_ready','awaiting_model','model_complete','awaiting_review') {
            $task = Set-LocalAiTaskState -Task $task -State $state
        }
        $task.State | Should -Be 'awaiting_review'
    }

    It 'rejects skipped and terminal transitions' {
        $task = New-LocalAiTask -Goal 'Test invalid transitions' -Mode implementation -RepositoryRoot $script:Repo
        { Set-LocalAiTaskState -Task $task -State model_complete } | Should -Throw '*Invalid task state transition*'
        $cancelled = Set-LocalAiTaskState -Task $task -State cancelled
        { Set-LocalAiTaskState -Task $cancelled -State context_ready } | Should -Throw '*Invalid task state transition*'
    }

    It 'allows failure from a nonterminal state and acceptance only after review' {
        $task = New-LocalAiTask -Goal 'Test terminal states' -Mode investigation -RepositoryRoot $script:Repo
        $failed = Set-LocalAiTaskState -Task $task -State failed -Data @{ Error = 'model unavailable' }
        $failed.State | Should -Be 'failed'
        $failed.Data.Error | Should -Be 'model unavailable'

        $task2 = New-LocalAiTask -Goal 'Test acceptance' -Mode investigation -RepositoryRoot $script:Repo
        { Set-LocalAiTaskState -Task $task2 -State accepted } | Should -Throw '*Invalid task state transition*'
    }

    It 'writes append-only redacted JSON Lines events' {
        $task = New-LocalAiTask -Goal 'Test events' -Mode investigation -RepositoryRoot $script:Repo
        Add-LocalAiTaskEvent -Task $task -Operation 'invoke-model' -Data @{ Token = 'task-test-secret'; Detail = 'safe' }
        Add-LocalAiTaskEvent -Task $task -Operation 'create-handoff' -Data @{ Detail = 'second' }

        $eventPath = Join-Path $task.TaskDirectory 'events.jsonl'
        $lines = @(Get-Content -LiteralPath $eventPath)
        $lines.Count | Should -Be 3
        ($lines -join "`n") | Should -Not -Match 'task-test-secret'
        ($lines[1] | ConvertFrom-Json).Data.Token | Should -Be '[REDACTED]'
        ($lines[2] | ConvertFrom-Json).Operation | Should -Be 'create-handoff'
    }

    It 'leaves the last valid task file unchanged when a transition is rejected' {
        $task = New-LocalAiTask -Goal 'Test durable state' -Mode investigation -RepositoryRoot $script:Repo
        $taskPath = Join-Path $task.TaskDirectory 'task.json'
        $before = Get-Content -Raw -LiteralPath $taskPath

        { Set-LocalAiTaskState -Task $task -State accepted } | Should -Throw

        Get-Content -Raw -LiteralPath $taskPath | Should -BeExactly $before
        Get-ChildItem -LiteralPath $task.TaskDirectory -Filter '*.tmp' | Should -BeNullOrEmpty
    }
}
