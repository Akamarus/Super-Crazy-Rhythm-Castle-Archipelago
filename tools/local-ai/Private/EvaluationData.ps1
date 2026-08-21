function Get-LocalAiDecisionLedgerPath {
    return 'tools/local-ai/evaluation/decisions.json'
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
