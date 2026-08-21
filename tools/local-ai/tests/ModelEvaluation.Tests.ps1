BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    $script:CasesFixture = Join-Path $PSScriptRoot '..\evaluation\cases.json'
    Import-Module $script:ModulePath -Force

    function New-ModelEvaluationRepository {
        param([Parameter(Mandatory)] [string] $Path)

        New-Item -ItemType Directory -Path (Join-Path $Path 'tools\local-ai\evaluation') -Force | Out-Null
        Copy-Item -LiteralPath $script:CasesFixture -Destination (Join-Path $Path 'tools\local-ai\evaluation\cases.json')
        Set-Content -LiteralPath (Join-Path $Path '.gitignore') -Value ".local-ai/`n"
        git -C $Path init --quiet
        git -C $Path config user.email 'tests@example.invalid'
        git -C $Path config user.name 'Bridge Tests'
        git -C $Path add .gitignore tools/local-ai/evaluation/cases.json
        git -C $Path commit --quiet -m baseline
        return $Path
    }

    function Get-ModelEvaluationGitSnapshot {
        param([Parameter(Mandatory)] [string] $RepositoryRoot)

        [pscustomobject]@{
            Status = @(git -C $RepositoryRoot status --porcelain=v1 --untracked-files=all)
            Head = [string] (git -C $RepositoryRoot rev-parse HEAD)
            Branches = @(git -C $RepositoryRoot for-each-ref --format='%(refname) %(objectname)' refs/heads)
            Worktrees = @(git -C $RepositoryRoot worktree list --porcelain)
        }
    }

    function New-RecommendationModelResult {
        param(
            [Parameter(Mandatory)] [string] $ModelId,
            [Parameter(Mandatory)] [int] $Score,
            [Parameter(Mandatory)] [int] $MaximumScore,
            [Parameter(Mandatory)] [bool] $Passed,
            [int] $ErrorCount = 0
        )

        [pscustomobject]@{
            ModelId = $ModelId
            Score = $Score
            MaximumScore = $MaximumScore
            Passed = $Passed
            ErrorCount = $ErrorCount
            Cases = @()
        }
    }
}

Describe 'Local AI model evaluation orchestration and reports' {
    BeforeEach {
        $env:OPENWEBUI_API_KEY = 'evaluation-report-secret'
        $script:Repo = New-ModelEvaluationRepository (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
    }

    It 'evaluates both models in fixed order, continues after a case failure, and writes secure atomic reports without Git mutations' {
        $before = Get-ModelEvaluationGitSnapshot -RepositoryRoot $script:Repo

        $script:Result = InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            $script:Calls = [Collections.Generic.List[object]]::new()
            Mock Invoke-OpenWebUiChat {
                $case = ($Messages[1].content -split '\r?\n', 2)[1] | ConvertFrom-Json
                $script:Calls.Add([pscustomobject]@{
                    ModelId = $Configuration.ModelId
                    CaseId = $case.id
                    Messages = $Messages
                })
                if ($Configuration.ModelId -eq 'jacks-assistant' -and $case.id -eq 'access_design') {
                    throw 'request failed with evaluation-report-secret'
                }
                $content = [ordered]@{
                    case_id = $case.id
                    answers = @($case.questions | ForEach-Object {
                        [ordered]@{ id = $_.id; value = $_.expected }
                    })
                    explanation = 'safe rationale containing evaluation-report-secret'
                } | ConvertTo-Json -Depth 10 -Compress
                [pscustomobject]@{
                    Content = $content
                    ResponseId = "response-$($Configuration.ModelId)-$($case.id)"
                    ModelId = $Configuration.ModelId
                }
            }

            $script:Result = Invoke-LocalAiModelEvaluation -RepositoryRoot $Repo -OpenWebUiTimeoutSec 17

            $script:Calls.Count | Should -Be 10
            @($script:Calls.ModelId | Select-Object -Unique) | Should -Be @('jacks-assistant', 'jacks-assistant-fast')
            @($script:Calls | Group-Object ModelId | ForEach-Object Count) | Should -Be @(5, 5)
            foreach ($call in $script:Calls) {
                $call.Messages.Count | Should -Be 2
                $call.Messages[0].role | Should -Be 'system'
                $call.Messages[0].content | Should -Match 'exact(?:ly)? one JSON object'
                $call.Messages[0].content | Should -Match '"case_id"'
                $call.Messages[0].content | Should -Match '"answers"'
                $call.Messages[0].content | Should -Match '"explanation"'
                $call.Messages[1].role | Should -Be 'user'
                $call.Messages[1].content | Should -Match ([regex]::Escape($call.CaseId))
            }
            Should -Invoke Invoke-OpenWebUiChat -Times 10 -Exactly -ParameterFilter { $TimeoutSec -eq 17 }

            @($script:Result.Models).Count | Should -Be 2
            @($script:Result.Models[0].Cases).Count | Should -Be 5
            @($script:Result.Models[1].Cases).Count | Should -Be 5
            $failed = @($script:Result.Models[0].Cases | Where-Object CaseId -eq 'access_design')[0]
            $failed.Passed | Should -BeFalse
            $failed.Score | Should -Be 0
            ($failed.Errors -join "`n") | Should -Match '\[REDACTED\]'
            ($failed.Errors -join "`n") | Should -Not -Match 'evaluation-report-secret'
            $script:Result.Recommendation | Should -Be 'jacks-assistant-fast'
            $script:Result.Reason | Should -Be 'perfect-winner'
            return $script:Result
        }

        $after = Get-ModelEvaluationGitSnapshot -RepositoryRoot $script:Repo
        @($after.Status) | Should -Be @($before.Status)
        $after.Head | Should -BeExactly $before.Head
        @($after.Branches) | Should -Be @($before.Branches)
        @($after.Worktrees) | Should -Be @($before.Worktrees)

        $stateRoot = [IO.Path]::GetFullPath((Join-Path $script:Repo '.local-ai'))
        $evaluationRoot = [IO.Path]::GetFullPath((Join-Path $stateRoot 'evaluations'))
        $script:Result.EvaluationDirectory.StartsWith($evaluationRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
        Split-Path -Leaf $script:Result.EvaluationDirectory | Should -Match '^\d{8}T\d{9}Z-[a-f0-9]{12}$'
        Test-Path -LiteralPath $script:Result.JsonReportPath -PathType Leaf | Should -BeTrue
        Test-Path -LiteralPath $script:Result.MarkdownReportPath -PathType Leaf | Should -BeTrue
        Split-Path -Leaf $script:Result.JsonReportPath | Should -Be 'report.json'
        Split-Path -Leaf $script:Result.MarkdownReportPath | Should -Be 'report.md'

        $jsonText = Get-Content -Raw -LiteralPath $script:Result.JsonReportPath
        $markdown = Get-Content -Raw -LiteralPath $script:Result.MarkdownReportPath
        { $jsonText | ConvertFrom-Json -ErrorAction Stop } | Should -Not -Throw
        $json = $jsonText | ConvertFrom-Json
        $json.SchemaVersion | Should -Be 1
        @($json.EvaluatedModelIds) | Should -Be @('jacks-assistant', 'jacks-assistant-fast')
        $json.CaseDefinitionHash | Should -Match '^[a-f0-9]{64}$'
        $json.Totals.ModelCount | Should -Be 2
        $json.Totals.CaseCount | Should -Be 5
        $json.Totals.QuestionCount | Should -Be 11
        $markdown | Should -Match 'Scores'
        $markdown | Should -Match 'Errors'
        $markdown | Should -Match 'jacks-assistant-fast'
        $markdown | Should -Match 'perfect-winner'
        $jsonText | Should -Not -Match 'evaluation-report-secret'
        $markdown | Should -Not -Match 'evaluation-report-secret'
        @(Get-ChildItem -LiteralPath $script:Result.EvaluationDirectory -Force -Filter '*.tmp') | Should -BeNullOrEmpty
        @(Get-ChildItem -LiteralPath $stateRoot -Force).Name | Should -Be @('evaluations')
        @(Get-ChildItem -LiteralPath $stateRoot -Recurse -File | Where-Object Name -in @('task.json','events.jsonl')) | Should -BeNullOrEmpty
    }

    It 'allows one explicit allowlisted model' {
        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo } {
            $script:CalledModels = [Collections.Generic.List[string]]::new()
            Mock Invoke-OpenWebUiChat {
                $case = ($Messages[1].content -split '\r?\n', 2)[1] | ConvertFrom-Json
                $script:CalledModels.Add([string] $Configuration.ModelId)
                [pscustomobject]@{
                    Content = ([ordered]@{
                        case_id = $case.id
                        answers = @($case.questions | ForEach-Object { [ordered]@{ id = $_.id; value = $_.expected } })
                        explanation = 'perfect'
                    } | ConvertTo-Json -Depth 10 -Compress)
                    ResponseId = 'subset-response'
                    ModelId = $Configuration.ModelId
                }
            }

            $result = Invoke-LocalAiModelEvaluation -RepositoryRoot $Repo -ModelId @('jacks-assistant-fast')

            @($result.EvaluatedModelIds) | Should -Be @('jacks-assistant-fast')
            @($script:CalledModels) | Should -Be (@('jacks-assistant-fast') * 5)
            $result.Recommendation | Should -Be 'jacks-assistant-fast'
        }
    }

    It 'rejects arbitrary or duplicate model IDs before HTTP' -ForEach @(
        @{ Name = 'arbitrary'; ModelId = @('other-model') },
        @{ Name = 'duplicate'; ModelId = @('jacks-assistant', 'jacks-assistant') }
    ) {
        param($Name, $ModelId)

        InModuleScope LocalAiBridge -Parameters @{ Repo = $script:Repo; RequestedModelId = $ModelId } {
            Mock Invoke-OpenWebUiChat { throw 'must not run' }

            { Invoke-LocalAiModelEvaluation -RepositoryRoot $Repo -ModelId $RequestedModelId } |
                Should -Throw

            Should -Invoke Invoke-OpenWebUiChat -Times 0
        }
    }

    It 'exports the public evaluation command' {
        Get-Command Invoke-LocalAiModelEvaluation -Module LocalAiBridge -ErrorAction Stop |
            Should -Not -BeNullOrEmpty
    }
}

Describe 'Local AI model evaluation recommendation' {
    It 'recommends only a sole perfect winner' -ForEach @(
        @{
            Name = 'fast wins'
            LegacyScore = 10
            LegacyPassed = $false
            FastScore = 11
            FastPassed = $true
            Expected = 'jacks-assistant-fast'
        },
        @{
            Name = 'legacy wins'
            LegacyScore = 11
            LegacyPassed = $true
            FastScore = 10
            FastPassed = $false
            Expected = 'jacks-assistant'
        }
    ) {
        param($Name, $LegacyScore, $LegacyPassed, $FastScore, $FastPassed, $Expected)

        $models = @(
            (New-RecommendationModelResult -ModelId 'jacks-assistant' -Score $LegacyScore -MaximumScore 11 -Passed $LegacyPassed),
            (New-RecommendationModelResult -ModelId 'jacks-assistant-fast' -Score $FastScore -MaximumScore 11 -Passed $FastPassed)
        )

        InModuleScope LocalAiBridge -Parameters @{ ModelResults = $models; ExpectedModel = $Expected } {
            $recommendation = Get-LocalAiEvaluationRecommendation -ModelResult $ModelResults

            $recommendation.Recommendation | Should -Be $ExpectedModel
            $recommendation.Reason | Should -Be 'perfect-winner'
        }
    }

    It 'returns no recommendation for a tie' {
        $models = @(
            (New-RecommendationModelResult -ModelId 'jacks-assistant' -Score 11 -MaximumScore 11 -Passed $true),
            (New-RecommendationModelResult -ModelId 'jacks-assistant-fast' -Score 11 -MaximumScore 11 -Passed $true)
        )
        InModuleScope LocalAiBridge -Parameters @{ ModelResults = $models } {
            $recommendation = Get-LocalAiEvaluationRecommendation -ModelResult $ModelResults

            $recommendation.Recommendation | Should -BeNullOrEmpty
            $recommendation.Reason | Should -Be 'tie'
        }
    }

    It 'returns no recommendation when the unique highest score is not perfect' {
        $models = @(
            (New-RecommendationModelResult -ModelId 'jacks-assistant' -Score 10 -MaximumScore 11 -Passed $false),
            (New-RecommendationModelResult -ModelId 'jacks-assistant-fast' -Score 9 -MaximumScore 11 -Passed $false)
        )
        InModuleScope LocalAiBridge -Parameters @{ ModelResults = $models } {
            $recommendation = Get-LocalAiEvaluationRecommendation -ModelResult $ModelResults

            $recommendation.Recommendation | Should -BeNullOrEmpty
            $recommendation.Reason | Should -Be 'no-perfect-winner'
        }
    }

    It 'does not ignore errors when deciding perfection' {
        $models = @(
            (New-RecommendationModelResult -ModelId 'jacks-assistant' -Score 11 -MaximumScore 11 -Passed $false -ErrorCount 1),
            (New-RecommendationModelResult -ModelId 'jacks-assistant-fast' -Score 10 -MaximumScore 11 -Passed $false)
        )
        InModuleScope LocalAiBridge -Parameters @{ ModelResults = $models } {
            $recommendation = Get-LocalAiEvaluationRecommendation -ModelResult $ModelResults

            $recommendation.Recommendation | Should -BeNullOrEmpty
            $recommendation.Reason | Should -Be 'no-perfect-winner'
        }
    }
}
