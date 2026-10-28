# Low-threshold extent of the BH light, approximating what the eye reads as
# "the feature" (includes the deep-red falloff). Windows PowerShell 5.1 has
# no Measure-Object -Median, so the cluster filter uses the plain mean and a
# fixed 430 px radius; percentiles come from sorted arrays.
param([string]$Paths, [string]$Tag = "extent:")
$files = $Paths -split ';'
Add-Type -AssemblyName System.Drawing
foreach ($f in $files) {
	$bmp = [System.Drawing.Bitmap]::FromFile($f)
	$h = $bmp.Height; $w = $bmp.Width
	$sys = New-Object System.Collections.Generic.List[object]
	for ($y = 0; $y -lt $h; $y++) {
		for ($x = 0; $x -lt $w; $x += 2) {
			$c = $bmp.GetPixel($x, $y)
			if ($c.R -gt 14 -and $c.G -lt ($c.R * 0.45) -and $c.B -lt ($c.R * 0.50)) {
				$sys.Add(@($x, $y))
			}
		}
	}
	if ($sys.Count -lt 100) { Write-Output "$Tag $f : only $($sys.Count) px"; $bmp.Dispose(); continue }
	$cx = ($sys | ForEach-Object { $_[0] } | Measure-Object -Average).Average
	$cy = ($sys | ForEach-Object { $_[1] } | Measure-Object -Average).Average
	$near = $sys | Where-Object { ([math]::Pow($_[0]-$cx,2) + [math]::Pow($_[1]-$cy,2)) -lt [math]::Pow(430,2) }
	$ysS = @($near | ForEach-Object { $_[1] } | Sort-Object)
	$xsS = @($near | ForEach-Object { $_[0] } | Sort-Object)
	$n = $ysS.Count
	$lo = [math]::Max(0, [math]::Floor($n * 0.001))
	$hi = [math]::Min($n - 1, [math]::Ceiling($n * 0.999) - 1)
	$ymin2 = $ysS[$lo]; $ymax2 = $ysS[$hi]
	$ymin = $ysS[0]; $ymax = $ysS[$n-1]
	$xmin = $xsS[0]; $xmax = $xsS[$n-1]
	$hf = [math]::Round(($ymax - $ymin) / $h * 100, 1)
	$hf2 = [math]::Round(($ymax2 - $ymin2) / $h * 100, 1)
	$wf = [math]::Round(($xmax - $xmin) / $w * 100, 1)
	Write-Output "$Tag $(Split-Path $f -Leaf): abs y $ymin..$ymax = ${hf}% | p99.8-trim y = ${hf2}% | px $($near.Count) | x ${wf}%"
	$bmp.Dispose()
}
# debug: extreme points
Add-Type -AssemblyName System.Drawing
$f = $args[0]
$bmp = [System.Drawing.Bitmap]::FromFile($f)
$h = $bmp.Height; $w = $bmp.Width
$hits = New-Object System.Collections.Generic.List[object]
for ($y = 0; $y -lt $h; $y++) {
	for ($x = 0; $x -lt $w; $x += 2) {
		$c = $bmp.GetPixel($x, $y)
		if ($c.R -gt 14 -and $c.G -lt ($c.R * 0.45) -and $c.B -lt ($c.R * 0.50)) { $hits.Add(@($x, $y, $c.R, $c.G, $c.B)) }
	}
}
$cx = ($hits | ForEach-Object { $_[0] } | Measure-Object -Average).Average
$cy = ($hits | ForEach-Object { $_[1] } | Measure-Object -Average).Average
$near = $hits | Where-Object { ([math]::Pow($_[0]-$cx,2) + [math]::Pow($_[1]-$cy,2)) -lt [math]::Pow(430,2) }
$top = $near | Sort-Object { $_[1] } | Select-Object -First 5
$bot = $near | Sort-Object { $_[1] } -Descending | Select-Object -First 5
Write-Output "--- top extreme pixels (x y r g b):"; $top | ForEach-Object { "$($_[0]) $($_[1]) $($_[2]) $($_[3]) $($_[4])" }
Write-Output "--- bottom extreme pixels:"; $bot | ForEach-Object { "$($_[0]) $($_[1]) $($_[2]) $($_[3]) $($_[4])" }
$bmp.Dispose()
