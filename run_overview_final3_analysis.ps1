$prompt = "Describe exactly what you see in this frame. Focus on: 1) How many ships or units visible? 2) Space background? 2) Asteroids? 3) Overall scene - are multiple units visible?"
$total = [System.Diagnostics.Stopwatch]::StartNew()
$frames = @(
    @{name="Overview 3 0s"; file="teaser_overview_final3_0s.jpg"; sec=0},
    @{name="Overview 3 1s"; file="teaser_overview_final3_1s.jpg"; sec=1},
    @{name="Overview 3 2s"; file="teaser_overview_final3_2s.jpg"; sec=2},
    @{name="Overview 3 3s"; file="teaser_overview_final3_3s.jpg"; sec=3},
    @{name="Overview 3 4s"; file="teaser_overview_final3_4s.jpg"; sec=4}
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