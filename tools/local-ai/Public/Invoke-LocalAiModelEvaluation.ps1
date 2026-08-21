function Invoke-LocalAiModelEvaluation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $RepositoryRoot,
        [string[]] $ModelId,
        [string] $ConfigPath,
        [ValidateRange(1, 600)] [int] $OpenWebUiTimeoutSec = 120
    )

    $allowedModelIds = @(Get-AllowedLocalAiModelId)
    $selectedModelIds = @(
        if ($null -eq $ModelId -or $ModelId.Count -eq 0) {
            $allowedModelIds
        } else {
            $ModelId
        }
    )
    $seenModelIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($selectedModelId in $selectedModelIds) {
        $isAllowed = @($allowedModelIds | Where-Object {
            $_.Equals($selectedModelId, [StringComparison]::Ordinal)
        }).Count -gt 0
        if (-not $isAllowed) {
            throw "ModelId must be allowlisted. Allowed values: 'jacks-assistant', 'jacks-assistant-fast'."
        }
        if (-not $seenModelIds.Add($selectedModelId)) {
            throw "ModelId values must not contain duplicates: $selectedModelId"
        }
    }

    $configuration = Get-LocalAiConfiguration -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    $cases = @(Get-LocalAiEvaluationCases -RepositoryRoot $configuration.RepositoryRoot)
    $casesPath = Resolve-ContainedPath -Root $configuration.RepositoryRoot -Path (Get-LocalAiEvaluationCasesPath) -MustExist
    $caseDefinitionHash = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($casesPath))
    ).ToLowerInvariant()

    $stateRelativePath = [IO.Path]::GetRelativePath($configuration.RepositoryRoot, $configuration.StateRoot).Replace('\', '/')
    $reportRelativePath = $stateRelativePath.TrimEnd('/') + '/evaluations/report.json'
    & git -C $configuration.RepositoryRoot check-ignore --quiet -- $reportRelativePath
    if ($LASTEXITCODE -ne 0) {
        throw "StateRoot must be ignored by Git before evaluation reports can be written: $($configuration.StateRoot)"
    }

    New-Item -ItemType Directory -Path $configuration.StateRoot -Force | Out-Null
    $stateRoot = Resolve-ContainedPath -Root $configuration.RepositoryRoot -Path $configuration.StateRoot -MustExist
    $evaluationsRootCandidate = Resolve-ContainedPath -Root $stateRoot -Path 'evaluations'
    New-Item -ItemType Directory -Path $evaluationsRootCandidate -Force | Out-Null
    $evaluationsRoot = Resolve-ContainedPath -Root $stateRoot -Path $evaluationsRootCandidate -MustExist
    do {
        $evaluationId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 12)
        $evaluationDirectory = Resolve-ContainedPath -Root $evaluationsRoot -Path $evaluationId
    } while (Test-Path -LiteralPath $evaluationDirectory)
    New-Item -ItemType Directory -Path $evaluationDirectory | Out-Null

    $startedAtUtc = [DateTime]::UtcNow.ToString('o')
    $systemPrompt = @'
Return exactly one JSON object and no Markdown or additional text. Use exactly this shape:
{
  "case_id": "<case id>",
  "answers": [{"id": "<question id>", "value": "<allowed value>"}],
  "explanation": "brief human-review rationale"
}
Include exactly one answer for every question, preserve each question id exactly, and choose only a listed allowed value.
'@.Trim()
    $modelResults = [Collections.Generic.List[object]]::new()
    foreach ($selectedModelId in $selectedModelIds) {
        $configurationValues = [ordered]@{}
        foreach ($property in $configuration.PSObject.Properties) {
            $configurationValues[$property.Name] = $property.Value
        }
        $configurationValues.ModelId = $selectedModelId
        $modelConfiguration = [pscustomobject] $configurationValues

        $caseResults = [Collections.Generic.List[object]]::new()
        foreach ($case in $cases) {
            $stopwatch = [Diagnostics.Stopwatch]::StartNew()
            try {
                $userPrompt = 'Evaluation case:' + [Environment]::NewLine + ($case | ConvertTo-Json -Depth 10 -Compress)
                $response = Invoke-OpenWebUiChat -Configuration $modelConfiguration -Messages @(
                    [pscustomobject]@{ role = 'system'; content = $systemPrompt },
                    [pscustomobject]@{ role = 'user'; content = $userPrompt }
                ) -TimeoutSec $OpenWebUiTimeoutSec
                $parsedResponse = ConvertFrom-LocalAiEvaluationResponse -Content $response.Content
                $measurement = Measure-LocalAiEvaluationCase -Case $case -Response $parsedResponse
                $stopwatch.Stop()
                $caseResults.Add([pscustomobject] [ordered]@{
                    CaseId = [string] $case.id
                    Passed = [bool] $measurement.Passed
                    Score = [int] $measurement.Score
                    MaximumScore = [int] $measurement.MaximumScore
                    DurationMs = [long] [Math]::Round($stopwatch.Elapsed.TotalMilliseconds)
                    ResponseId = ConvertTo-RedactedData $response.ResponseId
                    Answers = @(ConvertTo-RedactedData $measurement.Answers)
                    Explanation = ConvertTo-RedactedData $parsedResponse.explanation
                    Errors = @(ConvertTo-RedactedData $measurement.Errors)
                })
            } catch {
                $stopwatch.Stop()
                $caseResults.Add([pscustomobject] [ordered]@{
                    CaseId = [string] $case.id
                    Passed = $false
                    Score = 0
                    MaximumScore = @($case.questions).Count
                    DurationMs = [long] [Math]::Round($stopwatch.Elapsed.TotalMilliseconds)
                    ResponseId = $null
                    Answers = @()
                    Explanation = $null
                    Errors = @([string] (ConvertTo-RedactedData $_.Exception.Message))
                })
            }
        }

        $score = [int] (@($caseResults | Measure-Object -Property Score -Sum)[0].Sum)
        $maximumScore = [int] (@($caseResults | Measure-Object -Property MaximumScore -Sum)[0].Sum)
        $errorCount = [int] (@($caseResults | ForEach-Object { @($_.Errors).Count } | Measure-Object -Sum)[0].Sum)
        $passed = $caseResults.Count -eq $cases.Count -and @($caseResults | Where-Object { -not $_.Passed }).Count -eq 0
        $modelResults.Add([pscustomobject] [ordered]@{
            ModelId = $selectedModelId
            Passed = $passed
            Score = $score
            MaximumScore = $maximumScore
            ErrorCount = $errorCount
            Cases = @($caseResults)
        })
    }

    $recommendation = Get-LocalAiEvaluationRecommendation -ModelResult @($modelResults)
    $jsonReportPath = Resolve-ContainedPath -Root $evaluationDirectory -Path 'report.json'
    $markdownReportPath = Resolve-ContainedPath -Root $evaluationDirectory -Path 'report.md'
    $report = [pscustomobject] [ordered]@{
        SchemaVersion = 1
        EvaluationId = $evaluationId
        EvaluationDirectory = $evaluationDirectory
        JsonReportPath = $jsonReportPath
        MarkdownReportPath = $markdownReportPath
        StartedAtUtc = $startedAtUtc
        CompletedAtUtc = [DateTime]::UtcNow.ToString('o')
        EvaluatedModelIds = @($selectedModelIds)
        CaseDefinitionHash = $caseDefinitionHash
        Totals = [pscustomobject] [ordered]@{
            ModelCount = $selectedModelIds.Count
            CaseCount = $cases.Count
            QuestionCount = [int] (@($cases | ForEach-Object { @($_.questions).Count } | Measure-Object -Sum)[0].Sum)
            EvaluationCount = $selectedModelIds.Count * $cases.Count
        }
        Models = @($modelResults)
        Recommendation = $recommendation.Recommendation
        Reason = $recommendation.Reason
    }
    Write-LocalAiEvaluationReports -Report $report -EvaluationDirectory $evaluationDirectory | Out-Null
    return $report
}
