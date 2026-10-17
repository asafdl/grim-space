$prompt = "Describe exactly what you see in this frame. Focus on: 1) Are there ANY ships or units visible on the screen? If yes, describe how many and what they look like. 2) Is there text overlay? 3) Is there a space background? 4) Any UI elements like health bars? Describe the overall scene."
$total = [System.Diagnostics.Stopwatch]::StartNew()
$frames = @(
    @{name="Units 67s"; file="units_67s.jpg"; sec=67},
    @{name="Units 68s"; file="units_68s.jpg"; sec=68},
    @{name="Units 69s"; file="units_69s.jpg"; sec=69},
    @{name="Units 70s"; file="units_70s.jpg"; sec=70},
    @{name="Units 71s"; file="units_71s.jpg"; sec=71},
    @{name="Units 72s"; file="units_72s.jpg"; sec=72},
    @{name="Units 73s"; file="units_73s.jpg"; sec=73},
    @{name="Units 74s"; file="units_74s.jpg"; sec=74}
)
foreach ($f in $frames) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $output = & "C:\Users\User\AppData\Roaming\npm\opencode.ps1" run -f ("D:\grim-space-recordings\" + $f.file) $prompt 2>&1
    $sw.Stop()
    Write-Host ("=== " + $f.name + " (~{0}s) ===" -f $f.sec)
    Write-Host ("Runtime: {0:F2}s" -f $sw.Elapsed.TotalSeconds)
    $response = $output | Where-Object { $_ -notmatch '^>' -and $_ -notmatch '^build' -and $_ -notmatch '^\s*$' }
    $response
    Write-Host ""
}
$total.Stop()
Write-Host ("=== TOTAL RUNTIME: {0:F2} seconds for {1} frames ===" -f $total.Elapsed.TotalSeconds, $frames.Count)
