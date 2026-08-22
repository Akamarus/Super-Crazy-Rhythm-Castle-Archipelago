BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
}

Describe 'Local AI bridge configuration' {
    BeforeEach {
        Remove-Module LocalAiBridge -ErrorAction SilentlyContinue
    }

    It 'loads safe defaults without exposing the API key' {
        $repo = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path $repo -Force | Out-Null
        $env:OPENWEBUI_API_KEY = 'do-not-return-this'

        Import-Module $script:ModulePath -Force
        $config = Get-LocalAiConfiguration -RepositoryRoot $repo

        $config.OpenWebUiBaseUri.AbsoluteUri.TrimEnd('/') | Should -Be 'http://127.0.0.1:8080'
        $config.ModelId | Should -Be 'jacks-assistant'
        $config.RepositoryRoot | Should -Be ([IO.Path]::GetFullPath($repo))
        $config.StateRoot | Should -Be ([IO.Path]::GetFullPath((Join-Path $repo '.local-ai')))
        $config.PSObject.Properties.Name | Should -Not -Contain 'ApiKey'
        ($config | ConvertTo-Json -Depth 5) | Should -Not -Match 'do-not-return-this'
    }

    It 'returns the fixed local AI model allowlist' {
        Import-Module $script:ModulePath -Force

        @(Get-AllowedLocalAiModelId) | Should -Be @('jacks-assistant', 'jacks-assistant-fast')
    }

    It 'accepts the fast allowlisted model alias' {
        $repo = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path $repo -Force | Out-Null
        $configPath = Join-Path $TestDrive 'config.psd1'
        Set-Content -LiteralPath $configPath -Value "@{ ModelId = 'jacks-assistant-fast' }"

        Import-Module $script:ModulePath -Force
        $config = Get-LocalAiConfiguration -RepositoryRoot $repo -ConfigPath $configPath

        $config.ModelId | Should -Be 'jacks-assistant-fast'
    }

    It 'rejects a model outside the fixed allowlist' {
        $repo = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path $repo -Force | Out-Null
        $configPath = Join-Path $TestDrive 'config.psd1'
        Set-Content -LiteralPath $configPath -Value "@{ ModelId = 'other-model' }"

        Import-Module $script:ModulePath -Force
        { Get-LocalAiConfiguration -RepositoryRoot $repo -ConfigPath $configPath } |
            Should -Throw '*allowlist*jacks-assistant*jacks-assistant-fast*'
    }

    It 'applies known local overrides' {
        $repo = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path $repo -Force | Out-Null
        $configPath = Join-Path $TestDrive 'config.psd1'
        Set-Content -LiteralPath $configPath -Value "@{ StateRoot = '.state'; WorktreeRoot = '..\worktrees'; GameDir = 'C:\Games\Titus' }"

        Import-Module $script:ModulePath -Force
        $config = Get-LocalAiConfiguration -RepositoryRoot $repo -ConfigPath $configPath

        $config.StateRoot | Should -Be ([IO.Path]::GetFullPath((Join-Path $repo '.state')))
        $config.WorktreeRoot | Should -Be ([IO.Path]::GetFullPath((Join-Path $repo '..\worktrees')))
        $config.GameDir | Should -Be ([IO.Path]::GetFullPath('C:\Games\Titus'))
    }

    It 'rejects unknown configuration keys' {
        $repo = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path $repo -Force | Out-Null
        $configPath = Join-Path $TestDrive 'config.psd1'
        Set-Content -LiteralPath $configPath -Value "@{ ApiKey = 'forbidden' }"

        Import-Module $script:ModulePath -Force
        { Get-LocalAiConfiguration -RepositoryRoot $repo -ConfigPath $configPath } |
            Should -Throw '*Unknown configuration key*ApiKey*'
    }

    It 'rejects a state root outside the repository' {
        $repo = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path $repo -Force | Out-Null
        $configPath = Join-Path $TestDrive 'config.psd1'
        Set-Content -LiteralPath $configPath -Value "@{ StateRoot = '..\outside' }"

        Import-Module $script:ModulePath -Force
        { Get-LocalAiConfiguration -RepositoryRoot $repo -ConfigPath $configPath } |
            Should -Throw '*StateRoot must resolve inside RepositoryRoot*'
    }

    It 'rejects non-loopback API endpoints' {
        Import-Module $script:ModulePath -Force -ErrorAction Stop
        { Assert-LoopbackUri -Uri 'https://example.com:8080' } | Should -Throw '*loopback*'
    }

    It 'accepts supported loopback API endpoints' -ForEach @(
        'http://127.0.0.1:8080'
        'http://localhost:8080'
        'http://[::1]:8080'
    ) {
        Import-Module $script:ModulePath -Force -ErrorAction Stop
        (Assert-LoopbackUri -Uri $_).IsLoopback | Should -BeTrue
    }
}
