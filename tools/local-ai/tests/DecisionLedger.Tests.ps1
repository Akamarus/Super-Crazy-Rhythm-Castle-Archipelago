BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    $script:LedgerFixture = Join-Path $PSScriptRoot '..\evaluation\decisions.json'
    $script:EvidencePaths = @(
        'docs\PROGRESSION.md',
        'docs\PROJECT_OVERVIEW.md',
        'docs\HISTORICAL_GAMEPLAY_EVIDENCE.md'
    )
    Import-Module $script:ModulePath -Force

    function New-DecisionLedgerRepo {
        param([string] $Path)

        New-Item -ItemType Directory -Path (Join-Path $Path 'tools\local-ai\evaluation'), (Join-Path $Path 'docs') -Force | Out-Null
        Copy-Item -LiteralPath $script:LedgerFixture -Destination (Join-Path $Path 'tools\local-ai\evaluation\decisions.json')
        foreach ($evidencePath in $script:EvidencePaths) {
            Set-Content -LiteralPath (Join-Path $Path $evidencePath) -Value "Evidence: $evidencePath"
        }
        git -C $Path init --quiet
        git -C $Path config user.email 'tests@example.invalid'
        git -C $Path config user.name 'Bridge Tests'
        git -C $Path add tools/local-ai/evaluation/decisions.json docs
        git -C $Path commit --quiet -m baseline
        return $Path
    }

    function Set-TestLedger {
        param(
            [Parameter(Mandatory)] [string] $RepositoryRoot,
            [Parameter(Mandatory)] $Ledger
        )

        $path = Join-Path $RepositoryRoot 'tools\local-ai\evaluation\decisions.json'
        $Ledger | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $path
    }
}

Describe 'Local AI decisions ledger' {
    BeforeEach {
        $env:OPENWEBUI_API_KEY = 'ledger-secret'
        $script:Repo = New-DecisionLedgerRepo (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
    }

    It 'loads five unique decisions in file order without API key data' {
        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            Get-LocalAiDecisionLedgerPath | Should -Be 'tools/local-ai/evaluation/decisions.json'
            $ledger = Get-LocalAiDecisionLedger -RepositoryRoot $Repo

            @($ledger.decisions).Count | Should -Be 5
            @($ledger.decisions.id) | Should -Be @(
                'area-access-vs-level-access',
                'location-vs-item',
                'hip-glasses-chain',
                'historical-vs-current-design',
                'native-flag-confidence'
            )
            (@($ledger.decisions.id) | Select-Object -Unique).Count | Should -Be 5
            ($ledger | ConvertTo-Json -Depth 20) | Should -Not -Match 'ledger-secret'
        }
    }

    It 'rejects duplicate decision IDs' {
        $ledger = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\decisions.json') | ConvertFrom-Json
        $ledger.decisions += $ledger.decisions[0]
        Set-TestLedger -RepositoryRoot $script:Repo -Ledger $ledger

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiDecisionLedger -RepositoryRoot $Repo } | Should -Throw '*duplicate*'
        }
    }

    It 'rejects missing or empty approved statements' -ForEach @(
        @{ Name = 'missing'; Value = $null },
        @{ Name = 'empty'; Value = '' }
    ) {
        param($Name, $Value)
        $ledger = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\decisions.json') | ConvertFrom-Json
        if ($Name -eq 'missing') {
            $ledger.decisions[0].PSObject.Properties.Remove('approved_statement')
        } else {
            $ledger.decisions[0].approved_statement = $Value
        }
        Set-TestLedger -RepositoryRoot $script:Repo -Ledger $ledger

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiDecisionLedger -RepositoryRoot $Repo } | Should -Throw '*approved_statement*'
        }
    }

    It 'rejects missing or empty prohibited interpretation arrays' -ForEach @(
        @{ Name = 'missing'; Value = $null },
        @{ Name = 'empty'; Value = @() }
    ) {
        param($Name, $Value)
        $ledger = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\decisions.json') | ConvertFrom-Json
        if ($Name -eq 'missing') {
            $ledger.decisions[0].PSObject.Properties.Remove('prohibited_interpretations')
        } else {
            $ledger.decisions[0].prohibited_interpretations = $Value
        }
        Set-TestLedger -RepositoryRoot $script:Repo -Ledger $ledger

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiDecisionLedger -RepositoryRoot $Repo } | Should -Throw '*prohibited_interpretations*'
        }
    }

    It 'rejects absolute, traversal, untracked, and missing evidence paths' -ForEach @(
        @{ Name = 'absolute'; Path = 'C:\outside.md'; Expected = '*evidence path*' },
        @{ Name = 'traversal'; Path = '../outside.md'; Expected = '*evidence path*' },
        @{ Name = 'untracked'; Path = 'docs/untracked.md'; Expected = '*tracked*' },
        @{ Name = 'missing'; Path = 'docs/missing.md'; Expected = '*does not exist*' }
    ) {
        param($Name, $Path, $Expected)
        $ledger = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\decisions.json') | ConvertFrom-Json
        $ledger.decisions[0].evidence_paths = @($Path)
        if ($Name -eq 'untracked') {
            Set-Content -LiteralPath (Join-Path $script:Repo $Path) -Value 'not tracked'
        }
        Set-TestLedger -RepositoryRoot $script:Repo -Ledger $ledger

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo; Expected = $Expected } {
            { Get-LocalAiDecisionLedger -RepositoryRoot $Repo } | Should -Throw $Expected
        }
    }

    It 'rejects unsupported schema versions' {
        $ledger = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\decisions.json') | ConvertFrom-Json
        $ledger.schema_version = 2
        Set-TestLedger -RepositoryRoot $script:Repo -Ledger $ledger

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiDecisionLedger -RepositoryRoot $Repo } | Should -Throw '*schema_version*'
        }
    }

    It 'rejects a wrong-cased root property name' {
        $ledger = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\decisions.json') | ConvertFrom-Json
        $ledger.PSObject.Properties.Remove('schema_version')
        $ledger | Add-Member -NotePropertyName 'Schema_Version' -NotePropertyValue 1
        Set-TestLedger -RepositoryRoot $script:Repo -Ledger $ledger

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiDecisionLedger -RepositoryRoot $Repo } | Should -Throw '*exactly these properties*'
        }
    }

    It 'rejects a wrong-cased decision entry property name' {
        $ledger = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\decisions.json') | ConvertFrom-Json
        $ledger.decisions[0].PSObject.Properties.Remove('id')
        $ledger.decisions[0] | Add-Member -NotePropertyName 'ID' -NotePropertyValue 'area-access-vs-level-access'
        Set-TestLedger -RepositoryRoot $script:Repo -Ledger $ledger

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiDecisionLedger -RepositoryRoot $Repo } | Should -Throw '*exactly these properties*'
        }
    }
}
