BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
    function New-ContextRepo {
        param([string] $Path)
        New-Item -ItemType Directory -Path (Join-Path $Path 'docs') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $Path 'README.md') -Value 'alpha'
        Set-Content -LiteralPath (Join-Path $Path 'docs\PROGRESSION.md') -Value 'beta secret-context-value'
        Set-Content -LiteralPath (Join-Path $Path 'runtime.log') -Value 'excluded'
        git -C $Path init --quiet
        git -C $Path config user.email 'tests@example.invalid'
        git -C $Path config user.name 'Bridge Tests'
        git -C $Path add README.md docs/PROGRESSION.md runtime.log
        git -C $Path commit --quiet -m baseline
        return $Path
    }
}

Describe 'Bounded repository context' {
    BeforeEach {
        $env:OPENWEBUI_API_KEY = 'secret-context-value'
        $script:Repo = New-ContextRepo (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $script:Task = New-LocalAiTask -Goal 'context test' -Mode investigation -RepositoryRoot $script:Repo
    }

    It 'includes tracked text deterministically and redacts secrets' {
        $context = New-LocalAiContext -Task $script:Task -IncludePath @('docs/PROGRESSION.md','README.md') -MaxBytes 4096
        $context.Files.Path | Should -Be @('README.md','docs/PROGRESSION.md')
        $context.Text | Should -Match 'alpha'
        $context.Text | Should -Match 'beta \[REDACTED\]'
        $context.Text | Should -Not -Match 'secret-context-value'
    }

    It 'rejects excluded, untracked, binary, and outside paths' -ForEach @(
        'runtime.log', '.git/config', '.local-ai/task.json', '..\outside.txt', 'missing.txt', 'file.dll'
    ) {
        { New-LocalAiContext -Task $script:Task -IncludePath @($_) -MaxBytes 4096 } |
            Should -Throw
    }

    It 'enforces the byte limit before returning partial context' {
        { New-LocalAiContext -Task $script:Task -IncludePath @('README.md','docs/PROGRESSION.md') -MaxBytes 8 } |
            Should -Throw '*byte limit*'
    }
}
