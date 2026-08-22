function New-LocalAiContext {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Task,
        [Parameter(Mandatory)] [string[]] $IncludePath,
        [ValidateRange(1, 10485760)] [int] $MaxBytes = 262144
    )

    $excludedExtensions = @('.dll','.exe','.pdb','.zip','.rar','.7z','.apworld','.log','.bak')
    $uniquePaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($requestedPath in $IncludePath) { [void] $uniquePaths.Add($requestedPath.Replace('\','/')) }
    $orderedPaths = [Collections.Generic.List[string]]::new($uniquePaths)
    $orderedPaths.Sort([StringComparer]::Ordinal)
    $files = [Collections.Generic.List[object]]::new()
    $totalBytes = 0
    foreach ($relative in $orderedPaths) {
        $normalized = $relative.Replace('\','/')
        if ([IO.Path]::IsPathFullyQualified($relative) -or
            $normalized -match '(^|/)\.\.?(/|$)' -or
            $normalized -match '(^|/)(\.git|\.local-ai|bin|obj|dist)(/|$)' -or
            [IO.Path]::GetExtension($normalized).ToLowerInvariant() -in $excludedExtensions -or
            [Management.Automation.WildcardPattern]::ContainsWildcardCharacters($relative)) {
            throw "Context path is excluded: $relative"
        }
        $fullPath = Resolve-ContainedPath -Root $Task.RepositoryRoot -Path $relative -MustExist
        $tracked = & git -C $Task.RepositoryRoot ls-files --error-unmatch -- $normalized 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $tracked) {
            $tracked = @(& git -C $Task.RepositoryRoot ls-files | Where-Object {
                $_.Equals($normalized, [StringComparison]::OrdinalIgnoreCase)
            })
        }
        if ($LASTEXITCODE -ne 0 -or -not $tracked) {
            throw "Context path is not a tracked file: $relative"
        }
        $normalized = [string] @($tracked)[0]
        $content = [IO.File]::ReadAllText($fullPath, [Text.Encoding]::UTF8)
        $content = ConvertTo-RedactedData $content
        $bytes = [Text.Encoding]::UTF8.GetByteCount($content)
        if ($totalBytes + $bytes -gt $MaxBytes) {
            throw "Context byte limit exceeded by: $relative"
        }
        $totalBytes += $bytes
        $files.Add([pscustomobject]@{ Path = $normalized; Bytes = $bytes; Content = $content })
    }
    $text = ($files | ForEach-Object { "--- FILE: $($_.Path) ---`n$($_.Content)" }) -join "`n`n"
    return [pscustomobject]@{
        RepositoryRoot = $Task.RepositoryRoot
        BaselineCommit = $Task.BaselineCommit
        Files = @($files)
        TotalBytes = $totalBytes
        Text = $text
    }
}

function Assert-InvestigationResult {
    param(
        [Parameter(Mandatory)] $Result,
        [Parameter(Mandatory)] [string[]] $AllowedEvidencePath
    )
    $allowedEvidence = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $AllowedEvidencePath) {
        [void] $allowedEvidence.Add($path.Replace('\','/'))
    }
    foreach ($name in 'summary','findings','evidence','uncertainties','recommended_next_steps') {
        if (-not $Result.PSObject.Properties[$name]) {
            throw "Investigation result is missing required field: $name"
        }
    }
    if ($Result.summary -isnot [string]) { throw 'Investigation summary must be a string.' }
    foreach ($entry in @($Result.evidence)) {
        if (-not $entry.PSObject.Properties['path'] -or -not $entry.PSObject.Properties['detail']) {
            throw 'Each investigation evidence entry requires path and detail.'
        }
        if ($entry.path -isnot [string] -or -not $allowedEvidence.Contains($entry.path.Replace('\','/'))) {
            throw "Investigation evidence path is outside the selected context: $($entry.path)"
        }
    }
}
