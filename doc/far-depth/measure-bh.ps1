# Measures the on-screen extent of the sky-pass black hole in a still, in
# plain-pixel terms (fraction of frame height). BH pixels = the orange/red
# accretion light (r well above the star background, g well below r, b below r)
# clustered around the light's own centroid (immune to stray stars/UI).
param([string]$Path, [string]$Tag = "measure:")

Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile($Path)
$w = $bmp.Width; $h = $bmp.Height
$step = 1

# pass 1: collect light pixels
$pts = New-Object System.Collections.Generic.List[object]
for ($y = 0; $y -lt $h; $y += $step) {
	for ($x = 0; $x -lt $w; $x += $step) {
		$c = $bmp.GetPixel($x, $y)
		if ($c.R -gt 45 -and $c.G -lt ($c.R * 0.72) -and $c.B -lt ($c.R * 0.65)) {
			$pts.Add(@($x, $y))
		}
	}
}

if ($pts.Count -lt 50) { Write-Output "$Tag NOT ENOUGH LIGHT PIXELS ($($pts.Count))"; $bmp.Dispose(); exit 1 }

# centroid, then keep only pixels within a tight cone of the centroid's
# y-centre — stars/asteroid sprites are small clusters far away.
$cx = ($pts | ForEach-Object { $_[0] } | Measure-Object -Average).Average
$cy = ($pts | ForEach-Object { $_[1] } | Measure-Object -Average).Average
$near = $pts | Where-Object { $_ -ne $null -and ([math]::Pow($_[0]-$cx,2) + [math]::Pow($_[1]-$cy,2)) -lt [math]::Pow($h*0.25,2) }

$ys = $near | ForEach-Object { $_[1] }
$xs = $near | ForEach-Object { $_[0] }
$ymin = ($ys | Measure-Object -Minimum).Minimum
$ymax = ($ys | Measure-Object -Maximum).Maximum
$xmin = ($xs | Measure-Object -Minimum).Minimum
$xmax = ($xs | Measure-Object -Maximum).Maximum
$heightFrac = ($ymax - $ymin) / $h
$widthFrac  = ($xmax - $xmin) / $w
Write-Output "$Tag image ${w}x${h} light-pixels $($near.Count) centroid ($([math]::Round($cx,1)), $([math]::Round($cy,1)))"
Write-Output "$Tag bbox x ${xmin}..${xmax} y ${ymin}..${ymax} => height-fraction $([math]::Round($heightFrac*100,1))% width-fraction $([math]::Round($widthFrac*100,1))%"
$bmp.Dispose()
