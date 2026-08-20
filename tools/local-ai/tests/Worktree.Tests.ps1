BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force

    function New-WorktreeTestRepository {
        param([string] $Path)
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        git -C $Path init --quiet
        git -C $Path config user.email 'tests@example.invalid'
        git -C $Path config user.name 'Bridge Tests'
        Set-Content -LiteralPath (Join-Path $Path 'README.md') -Value '# baseline'
        git -C $Path add README.md
        git -C $Path commit --quiet -m baseline
        return $Path
    }
}

Describe 'Isolated implementation worktrees' {
    BeforeEach {
        $script:Repo = New-WorktreeTestRepository (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $script:WorktreeRoot = Join-Path $TestDrive ('worktrees-' + [guid]::NewGuid().ToString('N'))
        $script:ConfigPath = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '.psd1')
        Set-Content -LiteralPath $script:ConfigPath -Value "@{ WorktreeRoot = '$($script:WorktreeRoot.Replace("'","''"))' }"
        $script:CreatedPath = $null
    }

    AfterEach {
        if ($script:CreatedPath -and (Test-Path -LiteralPath $script:CreatedPath)) {
            git -C $script:Repo worktree remove --force $script:CreatedPath 2>$null
        }
        git -C $script:Repo worktree prune 2>$null
    }

    It 'creates and verifies a task branch at the exact baseline outside the repository' {
        $task = New-LocalAiTask -Goal 'Implement bridge policy' -Mode implementation -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath

        $result = New-LocalAiWorktree -TaskId $task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath
        $script:CreatedPath = $result.Path

        $result.Branch | Should -Be "ai/implement-bridge-policy-$($task.TaskId.Substring(0,6))"
        $result.BaselineCommit | Should -Be $task.BaselineCommit
        (git -C $result.Path rev-parse HEAD) | Should -Be $task.BaselineCommit
        $result.Path.StartsWith($script:Repo + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) | Should -BeFalse
        (Get-LocalAiWorktreeStatus -TaskId $task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath).Verified | Should -BeTrue
    }

    It 'rejects investigation tasks without creating a worktree' {
        $task = New-LocalAiTask -Goal 'Read only' -Mode investigation -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath
        { New-LocalAiWorktree -TaskId $task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath } |
            Should -Throw '*implementation task*'
        Test-Path -LiteralPath $script:WorktreeRoot | Should -BeFalse
    }

    It 'rejects an existing nonempty destination' {
        $task = New-LocalAiTask -Goal 'Occupied path' -Mode implementation -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath
        $expected = Join-Path $script:WorktreeRoot "$($task.Slug)-$($task.TaskId)"
        New-Item -ItemType Directory -Path $expected -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $expected 'owner.txt') -Value 'do not overwrite'

        { New-LocalAiWorktree -TaskId $task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath } |
            Should -Throw '*destination already exists*'
        Get-Content -LiteralPath (Join-Path $expected 'owner.txt') | Should -Be 'do not overwrite'
    }

    It 'rejects an unresolved recorded baseline' {
        $task = New-LocalAiTask -Goal 'Bad baseline' -Mode implementation -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath
        $task.BaselineCommit = '0000000000000000000000000000000000000000'
        $task | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $task.TaskDirectory 'task.json')

        { New-LocalAiWorktree -TaskId $task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath } |
            Should -Throw '*baseline*'
    }

    It 'detects mismatched worktree metadata without deleting the worktree' {
        $task = New-LocalAiTask -Goal 'Verify metadata' -Mode implementation -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath
        $created = New-LocalAiWorktree -TaskId $task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath
        $script:CreatedPath = $created.Path
        git -C $created.Path checkout --detach --quiet

        { Get-LocalAiWorktreeStatus -TaskId $task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:ConfigPath } |
            Should -Throw '*branch mismatch*'
        Test-Path -LiteralPath $created.Path | Should -BeTrue
    }
}
