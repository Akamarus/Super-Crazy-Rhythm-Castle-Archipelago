function Get-TaskFindings {
    param($Task)
    $path=Join-Path $Task.TaskDirectory 'findings.json'
    if(Test-Path -LiteralPath $path){return Get-Content -Raw -LiteralPath $path|ConvertFrom-Json}
    return $null
}

function Convert-HandoffToMarkdown {
    param($Handoff)
    $lines=@("# Local AI Task Handoff","","- Task: ``$($Handoff.TaskId)``","- Mode: ``$($Handoff.Mode)``","- State: ``$($Handoff.State)``","- Baseline: ``$($Handoff.BaselineCommit)``","","## Goal","",$Handoff.Goal,"","## Changed files","")
    if(@($Handoff.ChangedFiles).Count){$lines+=@($Handoff.ChangedFiles|ForEach-Object{"- ``$_``"})}else{$lines+='- None'}
    $lines+=@("","## Findings","")
    if($Handoff.Findings){$lines+="Summary: $($Handoff.Findings.summary)"}else{$lines+='No structured findings recorded.'}
    $lines+=@("","## Validation, build, and deployment evidence","")
    if(@($Handoff.Operations).Count){$lines+=@($Handoff.Operations|ForEach-Object{"- $($_.Operation) / $($_.Component): exit $($_.ExitCode)"})}else{$lines+='- None'}
    if(@($Handoff.Deployments).Count){$lines+=@($Handoff.Deployments|ForEach-Object{"- deployment to ``$($_.Destination)``: success=$($_.Succeeded)"})}
    $lines+=@("","## Risks and unresolved questions","")
    if(@($Handoff.UnresolvedQuestions).Count){$lines+=@($Handoff.UnresolvedQuestions|ForEach-Object{"- $_"})}else{$lines+='- None recorded'}
    $lines+=@("","## Human next actions","","- Review the findings, diff, and evidence.","- Accept or reject explicitly.","- Merge or push manually only if approved.")
    return ($lines -join [Environment]::NewLine)+[Environment]::NewLine
}
