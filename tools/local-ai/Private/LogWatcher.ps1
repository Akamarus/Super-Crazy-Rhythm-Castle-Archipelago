function Save-DevelopmentSessionRecord {
    param($Task,$Session)
    if ($Task.PSObject.Properties['DevelopmentSession']) { $Task.DevelopmentSession=$Session }
    else { $Task | Add-Member -NotePropertyName DevelopmentSession -NotePropertyValue $Session }
    $Task.UpdatedAtUtc=[DateTime]::UtcNow.ToString('o')
    Write-AtomicJson -Path (Join-Path $Task.TaskDirectory 'task.json') -Value $Task
}

$script:LogWatcherScript = {
    param($LogPath,$CapturePath,[long]$Offset,[string[]]$Patterns,[datetime]$DeadlineUtc,$TaskPath)
    while([DateTime]::UtcNow -lt $DeadlineUtc) {
        try {
            $state=(Get-Content -Raw -LiteralPath $TaskPath | ConvertFrom-Json).State
            if($state -in @('accepted','failed','cancelled')) { break }
            if(Test-Path -LiteralPath $LogPath) {
                $stream=[IO.File]::Open($LogPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
                try {
                    if($stream.Length -lt $Offset){$Offset=0}
                    [void]$stream.Seek($Offset,[IO.SeekOrigin]::Begin)
                    $reader=[IO.StreamReader]::new($stream,[Text.Encoding]::UTF8,$true,1024,$true)
                    try{$text=$reader.ReadToEnd()}finally{$reader.Dispose()}
                    $Offset=$stream.Position
                } finally {$stream.Dispose()}
                if($text) {
                    $lines=@($text -split '\r?\n' | Where-Object {$_})
                    if($Patterns.Count -gt 0) {$lines=@($lines | Where-Object {$line=$_; @($Patterns | Where-Object {$line -match [regex]::Escape($_)}).Count -gt 0})}
                    if($lines.Count -gt 0){[IO.File]::AppendAllLines($CapturePath,[string[]]$lines,[Text.UTF8Encoding]::new($false))}
                }
            }
        } catch { }
        Start-Sleep -Milliseconds 100
    }
}
