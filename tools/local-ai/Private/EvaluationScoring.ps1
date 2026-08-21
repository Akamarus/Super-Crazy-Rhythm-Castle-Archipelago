function ConvertFrom-LocalAiEvaluationResponse {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string] $Content)

    $json = $Content
    if ($json -cmatch '\A```json[ \t]*\r?\n(?<json>[\s\S]*?)\r?\n```\z') {
        $json = $Matches.json
    }

    try {
        $response = $json | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "Evaluation response is not valid JSON: $($_.Exception.Message)"
    }

    Assert-LocalAiExactProperties -Value $response -RequiredProperty @('case_id','answers','explanation') -Description 'Evaluation response'
    if ($response.case_id -isnot [string] -or [string]::IsNullOrWhiteSpace($response.case_id)) {
        throw 'Evaluation response case_id must be a non-empty string.'
    }
    if ($response.answers -isnot [Collections.IEnumerable] -or $response.answers -is [string]) {
        throw 'Evaluation response answers must be an array.'
    }
    if ($response.explanation -isnot [string]) {
        throw 'Evaluation response explanation must be a string.'
    }

    $answerIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($answer in @($response.answers)) {
        Assert-LocalAiExactProperties -Value $answer -RequiredProperty @('id','value') -Description 'Evaluation response answer'
        if ($answer.id -isnot [string] -or [string]::IsNullOrWhiteSpace($answer.id)) {
            throw 'Evaluation response answer id must be a non-empty string.'
        }
        if (-not $answerIds.Add($answer.id)) {
            throw "Evaluation response contains a duplicate answer id: $($answer.id)"
        }
        if ($answer.value -isnot [string]) {
            throw 'Evaluation response answer value must be a string.'
        }
    }

    return $response
}

function Get-LocalAiEvaluationProperty {
    param(
        [Parameter(Mandatory)] $Value,
        [Parameter(Mandatory)] [string] $Name
    )

    $property = $Value.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }
    return $property.Value
}

function Measure-LocalAiEvaluationCase {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Case,
        [Parameter(Mandatory)] $Response
    )

    $questions = @(Get-LocalAiEvaluationProperty -Value $Case -Name 'questions')
    $errors = [Collections.Generic.List[string]]::new()
    $answers = @(Get-LocalAiEvaluationProperty -Value $Response -Name 'answers')
    $caseId = Get-LocalAiEvaluationProperty -Value $Case -Name 'id'
    $responseCaseId = Get-LocalAiEvaluationProperty -Value $Response -Name 'case_id'
    if ($responseCaseId -isnot [string] -or -not [string]::Equals($responseCaseId, $caseId, [StringComparison]::Ordinal)) {
        $errors.Add("Response case_id does not match case id: $caseId")
    }

    $questionsById = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach ($question in $questions) {
        $questionsById[(Get-LocalAiEvaluationProperty -Value $question -Name 'id')] = $question
    }

    $seenAnswerIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $answeredQuestionIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $score = 0
    foreach ($answer in $answers) {
        $answerId = Get-LocalAiEvaluationProperty -Value $answer -Name 'id'
        $answerValue = Get-LocalAiEvaluationProperty -Value $answer -Name 'value'
        if ($answerId -isnot [string] -or [string]::IsNullOrWhiteSpace($answerId)) {
            $errors.Add('Response contains an answer without a string id.')
            continue
        }
        if (-not $seenAnswerIds.Add($answerId)) {
            $errors.Add("Response contains a duplicate answer id: $answerId")
            continue
        }
        if (-not $questionsById.ContainsKey($answerId)) {
            $errors.Add("Response contains an extra answer id: $answerId")
            continue
        }

        [void] $answeredQuestionIds.Add($answerId)
        $question = $questionsById[$answerId]
        $allowedValues = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($allowedValue in @(Get-LocalAiEvaluationProperty -Value $question -Name 'allowed_values')) {
            [void] $allowedValues.Add($allowedValue)
        }
        if ($answerValue -isnot [string] -or -not $allowedValues.Contains($answerValue)) {
            $errors.Add("Response answer value is not allowed for question $answerId")
            continue
        }
        if (-not [string]::Equals($answerValue, (Get-LocalAiEvaluationProperty -Value $question -Name 'expected'), [StringComparison]::Ordinal)) {
            $errors.Add("Response answer is incorrect for question $answerId")
            continue
        }
        $score++
    }

    foreach ($question in $questions) {
        $questionId = Get-LocalAiEvaluationProperty -Value $question -Name 'id'
        if (-not $answeredQuestionIds.Contains($questionId)) {
            $errors.Add("Response is missing an answer for question $questionId")
        }
    }

    $maximumScore = $questions.Count
    return [pscustomobject]@{
        Passed = ($score -eq $maximumScore -and $errors.Count -eq 0)
        Score = $score
        MaximumScore = $maximumScore
        Answers = $answers
        Errors = @($errors)
    }
}
