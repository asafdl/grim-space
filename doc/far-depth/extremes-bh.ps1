param([string]$Path)
Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile($Path)
$h = $bmp.Height; $w = $bmp.Width
$hits = New-Object System.Collections.Generic.List[object]
for ($y = 0; $y -lt $h; $y++) {
	for ($x = 0; $x -lt $w; $x += 2) {
		$c = $bmp.GetPixel($x, $y)
		if ($c.R -gt 14 -and $c.G -lt ($c.R * 0.70) -and $c.B -lt ($c.R * 0.62)) { $hits.Add(@($x, $y, $c.R, $c.G, $c.B)) }
	}
}
$cx = ($hits | ForEach-Object { $_[0] } | Measure-Object -Average).Average
$cy = ($hits | ForEach-Object { $_[1] } | Measure-Object -Average).Average
Write-Output "mean center ($([math]::Round($cx,1)), $([math]::Round($cy,1))), hits $($hits.Count)"
$near = $hits | Where-Object { ([math]::Pow($_[0]-$cx,2) + [math]::Pow($_[1]-$cy,2)) -lt [math]::Pow(430,2) }
$top = $near | Sort-Object { $_[1] } | Select-Object -First 4
$bot = $near | Sort-Object { $_[1] } -Descending | Select-Object -First 4
Write-Output "--- top extreme (x y r g b):"; $top | ForEach-Object { "$($_[0]) $($_[1]) $($_[2]) $($_[3]) $($_[4])" }
Write-Output "--- bottom extreme:"; $bot | ForEach-Object { "$($_[0]) $($_[1]) $($_[2]) $($_[3]) $($_[4])" }
$bmp.Dispose()
