BeforeAll {
    $script:ModulePath=Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
    function New-HandoffRepo([string]$Path){
        New-Item -ItemType Directory -Path $Path -Force|Out-Null; Set-Content (Join-Path $Path 'README.md') '# baseline'
        git -C $Path init --quiet; git -C $Path config user.email 'tests@example.invalid'; git -C $Path config user.name 'Bridge Tests'
        git -C $Path add README.md; git -C $Path commit --quiet -m baseline; return $Path
    }
    function Move-ToReview($Task){foreach($state in 'context_ready','awaiting_model','model_complete','awaiting_review'){$Task=Set-LocalAiTaskState -Task $Task -State $state};return $Task}
}

Describe 'Human review handoffs' {
    BeforeEach {
        $env:OPENWEBUI_API_KEY='handoff-secret'
        $script:Repo=New-HandoffRepo (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $script:WtRoot=Join-Path $TestDrive ('wt-'+[guid]::NewGuid().ToString('N'))
        $script:Config=Join-Path $TestDrive ([guid]::NewGuid().ToString('N')+'.psd1')
        Set-Content $script:Config "@{ WorktreeRoot = '$($script:WtRoot.Replace("'","''"))' }"
        $script:Task=New-LocalAiTask -Goal 'Review implementation' -Mode implementation -RepositoryRoot $script:Repo -ConfigPath $script:Config
        $script:Wt=New-LocalAiWorktree -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config
        Set-Content (Join-Path $script:Wt.Path 'README.md') '# changed'
        $script:Task=Move-ToReview (Get-LocalAiTask -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config)
    }
    AfterEach {if($script:Wt -and(Test-Path $script:Wt.Path)){git -C $script:Repo worktree remove --force $script:Wt.Path 2>$null};git -C $script:Repo worktree prune 2>$null}

    It 'creates complete redacted handoff artifacts without mutating Git' {
        $beforeHead=git -C $script:Wt.Path rev-parse HEAD; $beforeStatus=git -C $script:Wt.Path status --short; $beforeList=git -C $script:Repo worktree list --porcelain
        $handoff=New-LocalAiHandoff -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config
        $handoff.Goal|Should -Be 'Review implementation'; $handoff.BaselineCommit|Should -Be $script:Task.BaselineCommit
        $handoff.Worktree.Branch|Should -Be $script:Wt.Branch; $handoff.ChangedFiles|Should -Contain 'README.md'
        Test-Path (Join-Path $script:Task.TaskDirectory 'handoff.json')|Should -BeTrue
        Test-Path (Join-Path $script:Task.TaskDirectory 'handoff.md')|Should -BeTrue
        (Get-Content -Raw (Join-Path $script:Task.TaskDirectory 'handoff.md'))|Should -Not -Match 'handoff-secret'
        (git -C $script:Wt.Path rev-parse HEAD)|Should -Be $beforeHead
        (git -C $script:Wt.Path status --short)|Should -Be $beforeStatus
        (git -C $script:Repo worktree list --porcelain)|Should -Be $beforeList
    }

    It 'accepts only from awaiting review without performing Git integration' {
        $head=git -C $script:Wt.Path rev-parse HEAD
        $result=Complete-LocalAiReview -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config -Decision accept -Notes 'Approved'
        $result.State|Should -Be 'accepted'; $result.Review.Decision|Should -Be 'accept'
        (git -C $script:Wt.Path rev-parse HEAD)|Should -Be $head
        (git -C $script:Wt.Path status --short)|Should -Not -BeNullOrEmpty
    }

    It 'records rejection and redacts review notes while leaving state reviewable' {
        $result=Complete-LocalAiReview -TaskId $script:Task.TaskId -RepositoryRoot $script:Repo -ConfigPath $script:Config -Decision reject -Notes 'Contains handoff-secret'
        $result.State|Should -Be 'awaiting_review'; $result.Review.Decision|Should -Be 'reject'; $result.Review.Notes|Should -Be 'Contains [REDACTED]'
    }
}
