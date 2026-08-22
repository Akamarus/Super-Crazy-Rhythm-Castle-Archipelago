function Get-LocalAiEvaluationRecommendation {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $ModelResult)

    if ($ModelResult.Count -eq 0) {
        return [pscustomobject] [ordered]@{
            Recommendation = $null
            Reason = 'no-perfect-winner'
        }
    }

    $highestScore = @($ModelResult | Measure-Object -Property Score -Maximum)[0].Maximum
    $leaders = @($ModelResult | Where-Object { $_.Score -eq $highestScore })
    if ($leaders.Count -ne 1) {
        return [pscustomobject] [ordered]@{
            Recommendation = $null
            Reason = 'tie'
        }
    }

    $winner = $leaders[0]
    $isPerfect = $winner.Passed -and
        $winner.Score -eq $winner.MaximumScore -and
        [int] $winner.ErrorCount -eq 0
    if (-not $isPerfect) {
        return [pscustomobject] [ordered]@{
            Recommendation = $null
            Reason = 'no-perfect-winner'
        }
    }

    return [pscustomobject] [ordered]@{
        Recommendation = [string] $winner.ModelId
        Reason = 'perfect-winner'
    }
}

function ConvertTo-LocalAiEvaluationMarkdown {
    [CmdletBinding()]
    param([Parameter(Mandatory)] $Report)

    $safeReport = [pscustomobject] (ConvertTo-RedactedData $Report)
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add('# Local AI Model Evaluation')
    $lines.Add('')
    $lines.Add("- Evaluation: $($safeReport.EvaluationId)")
    $lines.Add("- Started (UTC): $($safeReport.StartedAtUtc)")
    $lines.Add("- Completed (UTC): $($safeReport.CompletedAtUtc)")
    $lines.Add("- Case definition SHA-256: $($safeReport.CaseDefinitionHash)")
    $lines.Add("- Recommendation: $(if ($safeReport.Recommendation) { $safeReport.Recommendation } else { 'none' })")
    $lines.Add("- Reason: $($safeReport.Reason)")
    $lines.Add('')
    $lines.Add('## Scores')
    $lines.Add('')
    $lines.Add('| Model | Score | Passed every case | Errors |')
    $lines.Add('| --- | ---: | :---: | ---: |')
    foreach ($model in @($safeReport.Models)) {
        $lines.Add("| $($model.ModelId) | $($model.Score)/$($model.MaximumScore) | $($model.Passed) | $($model.ErrorCount) |")
    }

    $lines.Add('')
    $lines.Add('## Errors')
    foreach ($model in @($safeReport.Models)) {
        $lines.Add('')
        $lines.Add("### $($model.ModelId)")
        $errors = @(
            foreach ($caseResult in @($model.Cases)) {
                foreach ($errorMessage in @($caseResult.Errors)) {
                    "- $($caseResult.CaseId): $errorMessage"
                }
            }
        )
        if ($errors.Count -eq 0) {
            $lines.Add('- None')
        } else {
            foreach ($errorLine in $errors) { $lines.Add($errorLine) }
        }
    }

    $lines.Add('')
    $lines.Add('## Per-case results')
    foreach ($model in @($safeReport.Models)) {
        $lines.Add('')
        $lines.Add("### $($model.ModelId)")
        $lines.Add('')
        $lines.Add('| Case | Score | Passed | Duration (ms) |')
        $lines.Add('| --- | ---: | :---: | ---: |')
        foreach ($caseResult in @($model.Cases)) {
            $lines.Add("| $($caseResult.CaseId) | $($caseResult.Score)/$($caseResult.MaximumScore) | $($caseResult.Passed) | $($caseResult.DurationMs) |")
        }
    }

    return ($lines -join [Environment]::NewLine) + [Environment]::NewLine
}

function Write-AtomicLocalAiEvaluationMarkdown {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Content
    )

    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $temporary = Join-Path $directory ('.' + [IO.Path]::GetFileName($Path) + '.' + [guid]::NewGuid().ToString('N') + '.tmp')
    try {
        $safeContent = [string] (ConvertTo-RedactedData $Content)
        [IO.File]::WriteAllText($temporary, $safeContent, [Text.UTF8Encoding]::new($false))
        [IO.File]::Move($temporary, $Path, $true)
    } finally {
        if (Test-Path -LiteralPath $temporary) {
            Remove-Item -LiteralPath $temporary -Force
        }
    }
}

function Write-LocalAiEvaluationReports {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Report,
        [Parameter(Mandatory)] [string] $EvaluationDirectory
    )

    $jsonPath = Resolve-ContainedPath -Root $EvaluationDirectory -Path 'report.json'
    $markdownPath = Resolve-ContainedPath -Root $EvaluationDirectory -Path 'report.md'
    Write-AtomicJson -Path $jsonPath -Value $Report
    $markdown = ConvertTo-LocalAiEvaluationMarkdown -Report $Report
    Write-AtomicLocalAiEvaluationMarkdown -Path $markdownPath -Content $markdown

    return [pscustomobject] [ordered]@{
        JsonReportPath = $jsonPath
        MarkdownReportPath = $markdownPath
    }
}
