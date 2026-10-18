param(
    [int]$StartFrame = 2000,
    [int]$EndFrame = 2300,
    [int]$BatchSize = 10,
    [int]$MaxParallel = 3
)

$frames = @()
for ($i = $StartFrame; $i -le $EndFrame; $i += 20) {
    $frames += $i
}

$batchCount = [math]::Ceiling($frames.Count / $BatchSize)
Write-Host "Total frames: $($frames.Count), Batches: $batchCount, Max parallel: $MaxParallel"

# Create batches
$batches = @()
for ($i = 0; $i -lt $frames.Count; $i += $BatchSize) {
    $batchFrames = $frames[$i..([math]::Min($i + $BatchSize - 1, $frames.Count - 1))]
    $batches += , $batchFrames
}

# Process in parallel groups
$results = @{}
$groupNum = 0
for ($i = 0; $i -lt $batches.Count; $i += $MaxParallel) {
    $groupNum++
    $group = $batches[$i..([math]::Min($i + $MaxParallel - 1, $batches.Count - 1))]
    Write-Host "`n=== Processing group $groupNum ($($group.Count) batches) ==="
    
    $jobs = @()
    foreach ($batch in $group) {
        $batchStr = $batch -join ", "
        $frameArgs = $batch | ForEach-Object { "-f `"D:\grim-space-recordings\combat_real3_all_$($_.ToString('D5')).jpg`"" }
        $cmd = "opencode run $($frameArgs -join ' ') `"How many ships visible? Any changes?`" 2>&1"
        $jobs += Start-Job -ScriptBlock { param($cmd) Invoke-Expression $cmd } -ArgumentList $cmd
    }
    
    # Wait for all jobs in this group
    $jobs | Wait-Job | Out-Null
    foreach ($job in $jobs) {
        $result = Receive-Job -Job $job
        $job | Remove-Job
        Write-Host $result
    }
}

Write-Host "`n=== Parallel analysis complete ==="
