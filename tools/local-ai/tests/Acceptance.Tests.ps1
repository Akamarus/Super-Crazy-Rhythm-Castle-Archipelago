BeforeAll {
    $script:ModulePath=Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
    function New-AcceptanceRepo([string]$Path){
        New-Item -ItemType Directory -Path (Join-Path $Path 'docs') -Force|Out-Null
        Set-Content (Join-Path $Path '.gitignore') '.local-ai/'
        Set-Content (Join-Path $Path 'README.md') '# SCRC test'
        Set-Content (Join-Path $Path 'docs\PROJECT_OVERVIEW.md') 'Level 4 glasses discovery next; Minim trade unknown.'
        git -C $Path init --quiet;git -C $Path config user.email 'tests@example.invalid';git -C $Path config user.name 'Bridge Tests'
        git -C $Path add .;git -C $Path commit --quiet -m baseline;return $Path
    }
}

Describe 'Investigation-only end-to-end acceptance' {
    It 'produces findings and handoff without tracked, branch, worktree, build, deployment, or watcher mutation' {
        $repo=New-AcceptanceRepo (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $task=New-LocalAiTask -Goal 'Investigate Level 4 glasses / Minim progression.' -Mode investigation -RepositoryRoot $repo
        $headBefore=git -C $repo rev-parse HEAD;$statusBefore=git -C $repo status --short;$worktreesBefore=git -C $repo worktree list --porcelain;$branchesBefore=git -C $repo branch --format='%(refname)'
        InModuleScope LocalAiBridge -Parameters @{Repo=$repo;TaskId=$task.TaskId}{
            Mock Invoke-OpenWebUiChat {[pscustomobject]@{Content='{"summary":"Evidence remains incomplete","findings":["The roadmap identifies the glasses and Minim trade as discovery targets"],"evidence":[{"path":"docs/PROJECT_OVERVIEW.md","detail":"Discovery is next and mapping is unknown"}],"uncertainties":["Exact native flags are unknown"],"recommended_next_steps":["Capture gameplay observations"]}';ResponseId='acceptance-mock';ModelId='jacks-assistant'}}
            Invoke-LocalAiInvestigation -TaskId $TaskId -RepositoryRoot $Repo -IncludePath @('README.md','docs/PROJECT_OVERVIEW.md')|Out-Null
            New-LocalAiHandoff -TaskId $TaskId -RepositoryRoot $Repo|Out-Null
        }
        (Get-LocalAiTask -TaskId $task.TaskId -RepositoryRoot $repo).State|Should -Be 'awaiting_review'
        Test-Path (Join-Path $task.TaskDirectory 'findings.json')|Should -BeTrue;Test-Path (Join-Path $task.TaskDirectory 'handoff.md')|Should -BeTrue
        (git -C $repo rev-parse HEAD)|Should -Be $headBefore;(git -C $repo status --short)|Should -Be $statusBefore
        (git -C $repo worktree list --porcelain)|Should -Be $worktreesBefore;(git -C $repo branch --format='%(refname)')|Should -Be $branchesBefore
        $events=Get-Content -Raw (Join-Path $task.TaskDirectory 'events.jsonl')
        $events|Should -Not -Match 'worktree-created|"Operation":"build"|"Operation":"deploy"|development-session-started'
    }
}
