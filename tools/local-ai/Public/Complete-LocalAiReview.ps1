function Complete-LocalAiReview {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TaskId,[Parameter(Mandatory)][string]$RepositoryRoot,[string]$ConfigPath,
        [Parameter(Mandatory)][ValidateSet('accept','reject')][string]$Decision,[Parameter(Mandatory)][string]$Notes
    )
    $task=Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    if($task.State -ne 'awaiting_review'){throw 'Review can only be completed from awaiting_review.'}
    if($Decision -eq 'accept'){$task=Set-LocalAiTaskState -Task $task -State accepted}
    $review=[pscustomobject]@{Decision=$Decision;Notes=(ConvertTo-RedactedData $Notes);ReviewedAtUtc=[DateTime]::UtcNow.ToString('o')}
    if($task.PSObject.Properties['Review']){$task.Review=$review}else{$task|Add-Member -NotePropertyName Review -NotePropertyValue $review}
    $task.UpdatedAtUtc=[DateTime]::UtcNow.ToString('o')
    Write-AtomicJson -Path (Join-Path $task.TaskDirectory 'task.json') -Value $task
    Add-LocalAiTaskEvent -Task $task -Operation 'review-completed' -Data @{Decision=$Decision;Notes=$Notes}
    return $task
}
