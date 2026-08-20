BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
    function New-InvestigationRepo {
        param([string] $Path)
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $Path 'README.md') -Value 'Level 4 glasses'
        git -C $Path init --quiet
        git -C $Path config user.email 'tests@example.invalid'
        git -C $Path config user.name 'Bridge Tests'
        git -C $Path add README.md
        git -C $Path commit --quiet -m baseline
        return $Path
    }
}

Describe 'Investigation workflow' {
    BeforeEach {
        $env:OPENWEBUI_API_KEY = 'investigation-secret'
        $script:Repo = New-InvestigationRepo (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $script:Task = New-LocalAiTask -Goal 'Investigate Level 4 glasses / Minim progression.' -Mode investigation -RepositoryRoot $script:Repo
    }

    It 'stores validated findings and reaches awaiting review' {
        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo; TaskId = $script:Task.TaskId } {
            Mock Invoke-OpenWebUiChat {
                [pscustomobject]@{ Content = '{"summary":"Mapped current evidence","findings":["Glasses are pending discovery"],"evidence":[{"path":"README.md","detail":"Level 4 glasses"}],"uncertainties":["Native flags unknown"],"recommended_next_steps":["Collect gameplay log"]}'; ResponseId = 'r1'; ModelId = 'jacks-assistant' }
            }
            $result = Invoke-LocalAiInvestigation -TaskId $TaskId -RepositoryRoot $Repo -IncludePath @('README.md')
            $result.summary | Should -Be 'Mapped current evidence'
            (Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $Repo).State | Should -Be 'awaiting_review'
            Test-Path (Join-Path $Repo ".local-ai\$TaskId\findings.json") | Should -BeTrue
            Should -Invoke Invoke-OpenWebUiChat -Times 1
        }
    }

    It 'accepts one surrounding JSON Markdown fence from the model' {
        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo; TaskId = $script:Task.TaskId } {
            Mock Invoke-OpenWebUiChat {
                [pscustomobject]@{ Content = @'
```json
{"summary":"Mapped fenced evidence","findings":[],"evidence":[],"uncertainties":[],"recommended_next_steps":[]}
```
'@; ResponseId = 'fenced-r1'; ModelId = 'jacks-assistant' }
            }

            $result = Invoke-LocalAiInvestigation -TaskId $TaskId -RepositoryRoot $Repo -IncludePath @('README.md')

            $result.summary | Should -Be 'Mapped fenced evidence'
            (Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $Repo).State | Should -Be 'awaiting_review'
        }
    }

    It 'fails closed on invalid model JSON' {
        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo; TaskId = $script:Task.TaskId } {
            Mock Invoke-OpenWebUiChat { [pscustomobject]@{ Content = 'not json'; ResponseId = 'r2'; ModelId = 'jacks-assistant' } }
            { Invoke-LocalAiInvestigation -TaskId $TaskId -RepositoryRoot $Repo -IncludePath @('README.md') } | Should -Throw
            (Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $Repo).State | Should -Be 'failed'
        }
    }

    It 'fails closed when model evidence cites a file outside the selected context' {
        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo; TaskId = $script:Task.TaskId } {
            Mock Invoke-OpenWebUiChat {
                [pscustomobject]@{ Content = '{"summary":"unsupported citation","findings":[],"evidence":[{"path":"docs/IDS.md","detail":"not supplied"}],"uncertainties":[],"recommended_next_steps":[]}'; ResponseId = 'outside-evidence-r1'; ModelId = 'jacks-assistant' }
            }

            { Invoke-LocalAiInvestigation -TaskId $TaskId -RepositoryRoot $Repo -IncludePath @('README.md') } |
                Should -Throw '*outside the selected context*'
            (Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $Repo).State | Should -Be 'failed'
        }
    }

    It 'rejects implementation tasks' {
        $implementation = New-LocalAiTask -Goal 'wrong mode' -Mode implementation -RepositoryRoot $script:Repo
        { Invoke-LocalAiInvestigation -TaskId $implementation.TaskId -RepositoryRoot $script:Repo -IncludePath @('README.md') } |
            Should -Throw '*investigation task*'
    }

    It 'forwards an explicit Open WebUI timeout for slower local models' {
        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo; TaskId = $script:Task.TaskId } {
            Mock Invoke-OpenWebUiChat {
                [pscustomobject]@{ Content = '{"summary":"ok","findings":[],"evidence":[],"uncertainties":[],"recommended_next_steps":[]}'; ResponseId = 'slow-r1'; ModelId = 'jacks-assistant' }
            }
            Invoke-LocalAiInvestigation -TaskId $TaskId -RepositoryRoot $Repo -IncludePath @('README.md') -OpenWebUiTimeoutSec 600 | Out-Null
            Should -Invoke Invoke-OpenWebUiChat -Times 1 -ParameterFilter { $TimeoutSec -eq 600 }
        }
    }
}
