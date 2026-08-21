function Get-LocalAiDecisionLedgerPath {
    return 'tools/local-ai/evaluation/decisions.json'
}

function Get-LocalAiEvaluationCasesPath {
    return 'tools/local-ai/evaluation/cases.json'
}

function Assert-LocalAiExactProperties {
    param(
        [Parameter(Mandatory)] $Value,
        [Parameter(Mandatory)] [string[]] $RequiredProperty,
        [Parameter(Mandatory)] [string] $Description
    )

    $actual = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($property in $Value.PSObject.Properties) {
        [void] $actual.Add($property.Name)
    }
    $required = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($propertyName in $RequiredProperty) {
        [void] $required.Add($propertyName)
    }
    $unexpected = @($actual | Where-Object { -not $required.Contains($_) })
    $missing = @($required | Where-Object { -not $actual.Contains($_) })
    if ($unexpected.Count -gt 0 -or $missing.Count -gt 0) {
        throw "$Description must contain exactly these properties: $($RequiredProperty -join ', ')."
    }
}

function Assert-LocalAiDecisionLedgerEvidencePath {
    param(
        [Parameter(Mandatory)] [string] $RepositoryRoot,
        [Parameter(Mandatory)] [string] $Path
    )

    $excludedExtensions = @('.dll','.exe','.pdb','.zip','.rar','.7z','.apworld','.log','.bak')
    if ([IO.Path]::IsPathFullyQualified($Path) -or
        $Path.Contains('\', [StringComparison]::Ordinal) -or
        $Path -match '(^|/)\.\.?(/|$)' -or
        $Path -match '(^|/)(\.git|\.local-ai|bin|obj|dist)(/|$)' -or
        [IO.Path]::GetExtension($Path).ToLowerInvariant() -in $excludedExtensions -or
        [Management.Automation.WildcardPattern]::ContainsWildcardCharacters($Path)) {
        throw "Decision ledger evidence path is not repository-relative: $Path"
    }

    $fullPath = Resolve-ContainedPath -Root $RepositoryRoot -Path $Path -MustExist
    $tracked = & git -C $RepositoryRoot ls-files --error-unmatch -- $Path 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $tracked) {
        throw "Decision ledger evidence path is not a tracked file: $Path"
    }

    return $fullPath
}

function Get-LocalAiDecisionLedger {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string] $RepositoryRoot)

    $ledgerPath = Get-LocalAiDecisionLedgerPath
    $fullPath = Resolve-ContainedPath -Root $RepositoryRoot -Path $ledgerPath -MustExist
    $tracked = & git -C $RepositoryRoot ls-files --error-unmatch -- $ledgerPath 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $tracked) {
        throw "Decision ledger is not a tracked file: $ledgerPath"
    }

    try {
        $ledger = [IO.File]::ReadAllText($fullPath, [Text.Encoding]::UTF8) | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "Decision ledger is not valid JSON: $($_.Exception.Message)"
    }

    Assert-LocalAiExactProperties -Value $ledger -RequiredProperty @('schema_version','decisions') -Description 'Decision ledger'
    if ($ledger.schema_version -isnot [long] -or [long] $ledger.schema_version -ne 1) {
        throw 'Decision ledger schema_version must be 1.'
    }
    if ($ledger.decisions -isnot [Collections.IEnumerable] -or $ledger.decisions -is [string] -or @($ledger.decisions).Count -eq 0) {
        throw 'Decision ledger decisions must be a non-empty array.'
    }

    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($decision in @($ledger.decisions)) {
        Assert-LocalAiExactProperties -Value $decision -RequiredProperty @('id','category','approved_statement','prohibited_interpretations','evidence_paths') -Description 'Decision ledger entry'
        foreach ($name in @('id','category','approved_statement')) {
            if ($decision.$name -isnot [string] -or [string]::IsNullOrWhiteSpace($decision.$name)) {
                throw "Decision ledger $name must be a non-empty string."
            }
        }
        if (-not $ids.Add($decision.id)) {
            throw "Decision ledger contains a duplicate id: $($decision.id)"
        }
        if ($decision.prohibited_interpretations -isnot [Collections.IEnumerable] -or
            $decision.prohibited_interpretations -is [string] -or
            @($decision.prohibited_interpretations).Count -eq 0 -or
            @($decision.prohibited_interpretations | Where-Object { $_ -isnot [string] -or [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) {
            throw 'Decision ledger prohibited_interpretations must be a non-empty string array.'
        }
        if ($decision.evidence_paths -isnot [Collections.IEnumerable] -or
            $decision.evidence_paths -is [string] -or
            @($decision.evidence_paths).Count -eq 0) {
            throw 'Decision ledger evidence_paths must be a non-empty string array.'
        }
        foreach ($evidencePath in @($decision.evidence_paths)) {
            if ($evidencePath -isnot [string] -or [string]::IsNullOrWhiteSpace($evidencePath)) {
                throw 'Decision ledger evidence_paths must be a non-empty string array.'
            }
            Assert-LocalAiDecisionLedgerEvidencePath -RepositoryRoot $RepositoryRoot -Path $evidencePath | Out-Null
        }
    }

    return [pscustomobject] (ConvertTo-RedactedData $ledger)
}

function Get-LocalAiEvaluationCases {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string] $RepositoryRoot)

    $casesPath = Get-LocalAiEvaluationCasesPath
    $fullPath = Resolve-ContainedPath -Root $RepositoryRoot -Path $casesPath -MustExist
    $tracked = & git -C $RepositoryRoot ls-files --error-unmatch -- $casesPath 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $tracked) {
        throw "Evaluation cases are not a tracked file: $casesPath"
    }

    try {
        $definition = [IO.File]::ReadAllText($fullPath, [Text.Encoding]::UTF8) | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "Evaluation cases are not valid JSON: $($_.Exception.Message)"
    }

    Assert-LocalAiExactProperties -Value $definition -RequiredProperty @('schema_version','cases') -Description 'Evaluation cases'
    if ($definition.schema_version -isnot [long] -or [long] $definition.schema_version -ne 1) {
        throw 'Evaluation cases schema_version must be 1.'
    }
    if ($definition.cases -isnot [Collections.IEnumerable] -or $definition.cases -is [string] -or @($definition.cases).Count -eq 0) {
        throw 'Evaluation cases must be a non-empty array.'
    }

    $caseIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($case in @($definition.cases)) {
        Assert-LocalAiExactProperties -Value $case -RequiredProperty @('id','prompt','questions') -Description 'Evaluation case'
        foreach ($name in @('id','prompt')) {
            if ($case.$name -isnot [string] -or [string]::IsNullOrWhiteSpace($case.$name)) {
                throw "Evaluation case $name must be a non-empty string."
            }
        }
        if (-not $caseIds.Add($case.id)) {
            throw "Evaluation cases contain a duplicate id: $($case.id)"
        }
        if ($case.questions -isnot [Collections.IEnumerable] -or $case.questions -is [string] -or @($case.questions).Count -eq 0) {
            throw 'Evaluation case questions must be a non-empty array.'
        }

        $questionIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($question in @($case.questions)) {
            Assert-LocalAiExactProperties -Value $question -RequiredProperty @('id','allowed_values','expected') -Description 'Evaluation question'
            if ($question.id -isnot [string] -or [string]::IsNullOrWhiteSpace($question.id)) {
                throw 'Evaluation question id must be a non-empty string.'
            }
            if (-not $questionIds.Add($question.id)) {
                throw "Evaluation case $($case.id) contains a duplicate question id: $($question.id)"
            }
            if ($question.allowed_values -isnot [Collections.IEnumerable] -or $question.allowed_values -is [string] -or @($question.allowed_values).Count -eq 0) {
                throw 'Evaluation question allowed_values must be a non-empty string array.'
            }
            $allowedValues = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            foreach ($allowedValue in @($question.allowed_values)) {
                if ($allowedValue -isnot [string] -or [string]::IsNullOrWhiteSpace($allowedValue)) {
                    throw 'Evaluation question allowed_values must be a non-empty string array.'
                }
                if (-not $allowedValues.Add($allowedValue)) {
                    throw "Evaluation question allowed_values contains a duplicate value: $allowedValue"
                }
            }
            if ($question.expected -isnot [string] -or -not $allowedValues.Contains($question.expected)) {
                throw 'Evaluation question expected must be one of its allowed_values.'
            }
        }
    }

    return @($definition.cases)
}
