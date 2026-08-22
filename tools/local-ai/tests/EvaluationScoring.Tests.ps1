BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    $script:CasesFixture = Join-Path $PSScriptRoot '..\evaluation\cases.json'
    Import-Module $script:ModulePath -Force

    function New-EvaluationRepo {
        param([Parameter(Mandatory)] [string] $Path)

        New-Item -ItemType Directory -Path (Join-Path $Path 'tools\local-ai\evaluation') -Force | Out-Null
        Copy-Item -LiteralPath $script:CasesFixture -Destination (Join-Path $Path 'tools\local-ai\evaluation\cases.json')
        git -C $Path init --quiet
        git -C $Path config user.email 'tests@example.invalid'
        git -C $Path config user.name 'Bridge Tests'
        git -C $Path add tools/local-ai/evaluation/cases.json
        git -C $Path commit --quiet -m baseline
        return $Path
    }

    function Set-TestCases {
        param(
            [Parameter(Mandatory)] [string] $RepositoryRoot,
            [Parameter(Mandatory)] $Cases
        )

        $Cases | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $RepositoryRoot 'tools\local-ai\evaluation\cases.json')
    }

    function New-PerfectResponse {
        param([Parameter(Mandatory)] $Case)

        [pscustomobject]@{
            case_id = $Case.id
            answers = @($Case.questions | ForEach-Object {
                [pscustomobject]@{ id = $_.id; value = $_.expected }
            })
            explanation = 'This explanation is not scored.'
        }
    }
}

Describe 'Local AI evaluation cases' {
    BeforeEach {
        $script:Repo = New-EvaluationRepo (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
    }

    It 'loads five ordered exact-answer cases' {
        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            $cases = @(Get-LocalAiEvaluationCases -RepositoryRoot $Repo)

            $cases.Count | Should -Be 5
            @($cases.id) | Should -Be @(
                'access_design',
                'location_item_mapping',
                'hip_glasses_chain',
                'historical_evidence',
                'native_flag_confidence'
            )
            @($cases | ForEach-Object { @($_.questions).Count } | Measure-Object -Sum).Sum | Should -Be 11
        }
    }

    It 'rejects duplicate case IDs' {
        $cases = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\cases.json') | ConvertFrom-Json
        $cases.cases += $cases.cases[0]
        Set-TestCases -RepositoryRoot $script:Repo -Cases $cases

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiEvaluationCases -RepositoryRoot $Repo } | Should -Throw '*duplicate*'
        }
    }

    It 'rejects duplicate question IDs within a case' {
        $cases = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\cases.json') | ConvertFrom-Json
        $cases.cases[0].questions[1].id = $cases.cases[0].questions[0].id
        Set-TestCases -RepositoryRoot $script:Repo -Cases $cases

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiEvaluationCases -RepositoryRoot $Repo } | Should -Throw '*duplicate*'
        }
    }

    It 'rejects a missing prompt' {
        $cases = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\cases.json') | ConvertFrom-Json
        $cases.cases[0].PSObject.Properties.Remove('prompt')
        Set-TestCases -RepositoryRoot $script:Repo -Cases $cases

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiEvaluationCases -RepositoryRoot $Repo } | Should -Throw '*exactly these properties*'
        }
    }

    It 'rejects empty allowed values' {
        $cases = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\cases.json') | ConvertFrom-Json
        $cases.cases[0].questions[0].allowed_values = @()
        Set-TestCases -RepositoryRoot $script:Repo -Cases $cases

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiEvaluationCases -RepositoryRoot $Repo } | Should -Throw '*allowed_values*'
        }
    }

    It 'rejects an expected value outside the answer enum' {
        $cases = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\cases.json') | ConvertFrom-Json
        $cases.cases[0].questions[0].expected = 'maybe'
        Set-TestCases -RepositoryRoot $script:Repo -Cases $cases

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiEvaluationCases -RepositoryRoot $Repo } | Should -Throw '*expected*'
        }
    }

    It 'rejects a case without questions' {
        $cases = Get-Content -Raw (Join-Path $script:Repo 'tools\local-ai\evaluation\cases.json') | ConvertFrom-Json
        $cases.cases[0].questions = @()
        Set-TestCases -RepositoryRoot $script:Repo -Cases $cases

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            { Get-LocalAiEvaluationCases -RepositoryRoot $Repo } | Should -Throw '*questions*'
        }
    }
}

Describe 'Local AI evaluation response parsing' {
    It 'accepts plain JSON and one surrounding json Markdown fence' -ForEach @(
        @{ Name = 'plain'; Content = '{"case_id":"access_design","answers":[{"id":"roots_access_required","value":"yes"}],"explanation":"plain"}' },
        @{ Name = 'fenced'; Content = ((([char]96).ToString() * 3) -join '') + 'json' + [Environment]::NewLine + '{"case_id":"access_design","answers":[{"id":"roots_access_required","value":"yes"}],"explanation":"fenced"}' + [Environment]::NewLine + ((([char]96).ToString() * 3) -join '') }
    ) {
        param($Name, $Content)

        InModuleScope LocalAiBridge -Parameters @{ Content = $Content } {
            $response = ConvertFrom-LocalAiEvaluationResponse -Content $Content

            $response.case_id | Should -Be 'access_design'
            $response.answers[0].id | Should -Be 'roots_access_required'
            $response.answers[0].value | Should -Be 'yes'
        }
    }

    It 'accepts one lowercase json Markdown fence surrounded by whitespace' {
        $fence = (([char]96).ToString() * 3) -join ''
        $content = [Environment]::NewLine + "  $fence" + 'json' + [Environment]::NewLine + '{"case_id":"access_design","answers":[{"id":"roots_access_required","value":"yes"}],"explanation":"fenced"}' + [Environment]::NewLine + $fence + [Environment]::NewLine + "`t "

        InModuleScope LocalAiBridge -Parameters @{ Content = $content } {
            $response = ConvertFrom-LocalAiEvaluationResponse -Content $Content

            $response.case_id | Should -Be 'access_design'
            $response.answers[0].id | Should -Be 'roots_access_required'
            $response.answers[0].value | Should -Be 'yes'
        }
    }

    It 'rejects non-literal json Markdown fence labels' -ForEach @(
        @{ Label = 'JSON' },
        @{ Label = 'Json' }
    ) {
        param($Label)

        $fence = (([char]96).ToString() * 3) -join ''
        $content = $fence + $Label + [Environment]::NewLine + '{"case_id":"access_design","answers":[{"id":"roots_access_required","value":"yes"}],"explanation":"x"}' + [Environment]::NewLine + $fence
        InModuleScope LocalAiBridge -Parameters @{ Content = $content } {
            { ConvertFrom-LocalAiEvaluationResponse -Content $Content } | Should -Throw '*valid JSON*'
        }
    }

    It 'rejects malformed JSON' {
        InModuleScope LocalAiBridge {
            { ConvertFrom-LocalAiEvaluationResponse -Content 'not json' } | Should -Throw '*valid JSON*'
        }
    }

    It 'rejects a non-string case ID' {
        InModuleScope LocalAiBridge {
            { ConvertFrom-LocalAiEvaluationResponse -Content '{"case_id":1,"answers":[{"id":"a","value":"yes"}],"explanation":"x"}' } | Should -Throw '*case_id*'
        }
    }

    It 'rejects missing answers' {
        InModuleScope LocalAiBridge {
            { ConvertFrom-LocalAiEvaluationResponse -Content '{"case_id":"access_design","explanation":"x"}' } | Should -Throw '*exactly these properties*'
        }
    }

    It 'rejects duplicate answer IDs' {
        InModuleScope LocalAiBridge {
            { ConvertFrom-LocalAiEvaluationResponse -Content '{"case_id":"access_design","answers":[{"id":"a","value":"yes"},{"id":"a","value":"no"}],"explanation":"x"}' } | Should -Throw '*duplicate*'
        }
    }

    It 'rejects non-string answer values' {
        InModuleScope LocalAiBridge {
            { ConvertFrom-LocalAiEvaluationResponse -Content '{"case_id":"access_design","answers":[{"id":"a","value":true}],"explanation":"x"}' } | Should -Throw '*value*'
        }
    }
}

Describe 'Local AI evaluation scoring' {
    BeforeAll {
        $script:Repo = New-EvaluationRepo (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
        $script:Case = & (Get-Module LocalAiBridge) {
            param($RepositoryRoot)
            @(Get-LocalAiEvaluationCases -RepositoryRoot $RepositoryRoot | Where-Object id -eq 'access_design')[0]
        } $script:Repo
    }

    It 'awards one point for every exact expected answer' {
        $response = New-PerfectResponse -Case $script:Case
        InModuleScope LocalAiBridge -Parameters @{ Case = $script:Case; Response = $response } {
            $result = Measure-LocalAiEvaluationCase -Case $Case -Response $Response

            $result.Passed | Should -BeTrue
            $result.Score | Should -Be 2
            $result.MaximumScore | Should -Be 2
            @($result.Errors).Count | Should -Be 0
        }
    }

    It 'fails wrong answers with a descriptive error' {
        $response = New-PerfectResponse -Case $script:Case
        $response.answers[0].value = 'no'
        InModuleScope LocalAiBridge -Parameters @{ Case = $script:Case; Response = $response } {
            $result = Measure-LocalAiEvaluationCase -Case $Case -Response $response

            $result.Passed | Should -BeFalse
            $result.Score | Should -Be 1
            @($result.Errors) | Should -Match 'roots_access_required'
        }
    }

    It 'fails missing answers with a descriptive error' {
        $response = New-PerfectResponse -Case $script:Case
        $response.answers = @($response.answers | Select-Object -Skip 1)
        InModuleScope LocalAiBridge -Parameters @{ Case = $script:Case; Response = $response } {
            $result = Measure-LocalAiEvaluationCase -Case $Case -Response $response

            $result.Passed | Should -BeFalse
            $result.Score | Should -Be 1
            @($result.Errors) | Should -Match 'missing.*roots_access_required'
        }
    }

    It 'fails extra, duplicate, and out-of-enum answers with descriptive errors' -ForEach @(
        @{ Name = 'extra'; Response = { param($Case) [pscustomobject]@{ case_id = $Case.id; answers = @((New-PerfectResponse -Case $Case).answers + [pscustomobject]@{ id = 'unasked'; value = 'yes' }); explanation = 'x' } }; Expected = 'extra.*unasked' },
        @{ Name = 'duplicate'; Response = { param($Case) $response = New-PerfectResponse -Case $Case; $response.answers += [pscustomobject]@{ id = 'roots_access_required'; value = 'yes' }; $response }; Expected = 'duplicate.*roots_access_required' },
        @{ Name = 'out of enum'; Response = { param($Case) $response = New-PerfectResponse -Case $Case; $response.answers[0].value = 'maybe'; $response }; Expected = 'not allowed.*roots_access_required' }
    ) {
        param($Name, $Response, $Expected)

        $responseValue = & $Response $script:Case
        InModuleScope LocalAiBridge -Parameters @{ Case = $script:Case; Response = $responseValue; Expected = $Expected } {
            $result = Measure-LocalAiEvaluationCase -Case $Case -Response $Response

            $result.Passed | Should -BeFalse
            @($result.Errors) | Should -Match $Expected
        }
    }

    It 'uses ordinal answer IDs rather than treating a differently cased ID as equivalent' {
        $response = New-PerfectResponse -Case $script:Case
        $response.answers[0].id = 'ROOTS_ACCESS_REQUIRED'
        InModuleScope LocalAiBridge -Parameters @{ Case = $script:Case; Response = $response } {
            $result = Measure-LocalAiEvaluationCase -Case $Case -Response $Response

            $result.Passed | Should -BeFalse
            $result.Score | Should -Be 1
            ($result.Errors -join "`n") | Should -Match 'extra.*ROOTS_ACCESS_REQUIRED'
            ($result.Errors -join "`n") | Should -Match 'missing.*roots_access_required'
        }
    }

    It 'does not let the free-form explanation change the score' {
        $first = New-PerfectResponse -Case $script:Case
        $second = New-PerfectResponse -Case $script:Case
        $first.explanation = 'A short explanation.'
        $second.explanation = 'An unrelated, incorrect, and much longer explanation that must not be scored.'
        InModuleScope LocalAiBridge -Parameters @{ Case = $script:Case; First = $first; Second = $second } {

            $firstResult = Measure-LocalAiEvaluationCase -Case $Case -Response $First
            $secondResult = Measure-LocalAiEvaluationCase -Case $Case -Response $Second

            $firstResult.Score | Should -Be $secondResult.Score
            $firstResult.Passed | Should -Be $secondResult.Passed
            @($firstResult.Errors) | Should -Be @($secondResult.Errors)
        }
    }
}
