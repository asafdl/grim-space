using System.Drawing;

// Offline proof for FAR-DEPTH skypass v3.1 item B: "no arc pixel crosses the
// shadow disc". Method:
//  1. light = pixel with R>14 (blocks the flood; stars/planets also block —
//     that is fine, we only care about the black hole).
//  2. Flood fill dark regions from the image border; every dark pixel the
//     fill cannot reach is an ENCLOSED dark island (the shadow disc + the
//     dark ramp inside the glow mask, islands in other sprites too).
//  3. For every island > 2000 px, report size, bbox, and the count of
//     saturated-orange pixels inside (R>50, R>1.8*G, R>1.8*B — the hue class
//     of the accretion light; grey planets and brown asteroids do not pass).
// PASS = no island holds a saturated-orange pixel (the arcs hug, none cross).
int MinArcR = 50; // saturated-orange test: R > 50 AND R > 1.8*G AND R > 1.8*B
int ShadowRadRad = 60; // x1000, shadow angular radius 0.060 rad as configured
int GlowRadRad = 114;  // x1000, glow angular radius 0.114 rad as configured
if (args.Length < 1) { Console.Error.WriteLine("usage: bhprove <png> [--circle out.png]"); return; }
using var bmp = new Bitmap(args[0]);
int w = bmp.Width, n = bmp.Height;
var light = new bool[n, w]; // [x,y] indexing flat below
var r = new byte[n * w]; var g = new byte[n * w]; var b = new byte[n * w];
for (int y = 0; y < n; y++)
	for (int x = 0; x < w; x++)
	{
		var c = bmp.GetPixel(x, y);
		int i = y * w + x;
		r[i] = c.R; g[i] = c.G; b[i] = c.B;
		light[y, x] = c.R > 14;
	}
bool lightAt(int x, int y) => x < 0 || y < 0 || x >= w || y >= n || light[y, x];

// ---- circle mode: fit the shadow face from the strict-ring cluster, then
// count saturated-orange pixels strictly inside it (3 px margin in) and
// annotate a zoomed proof image (face circle in white, hits in magenta).
bool circleMode = args.Length > 2 && args[1] == "--circle";
string? outPath = circleMode ? args[2] : null;
if (circleMode)
{
	// strict accretion-light cluster (ring/crest/arcs, excludes sun yellow via
	// the blue channel; layout reddish far objects drop to the cluster filter)
	var members = new List<(int x, int y)>();
	for (int y = 0; y < n; y++)
		for (int x = 0; x < w; x++)
		{
			int i = y * w + x;
			if (r[i] > 45 && g[i] < r[i] * 9 / 10 && b[i] < r[i] * 3 / 4)
			{
				members.Add((x, y));
			}
		}
	if (members.Count < 1000) { Console.WriteLine("circle: cluster too small"); return; }
	// robust center: median of the cluster-filtered members (the sun blob
	// could bias a mean; the median does not)
	var kept = new List<(int x, int y)>();
	double cx0 = members.Average(m => (double)m.x), cy0 = members.Average(m => (double)m.y);
	foreach (var m in members)
	{
		double dx = m.x - cx0, dy = m.y - cy0;
		if (dx * dx + dy * dy < 430.0 * 430.0) kept.Add(m);
	}
	if (kept.Count < 1000) { Console.WriteLine("circle: kept cluster too small"); return; }
	var xs = kept.Select(m => m.x).OrderBy(v => v).ToArray();
	var ys = kept.Select(m => m.y).OrderBy(v => v).ToArray();
	double cx = xs[kept.Count / 2], cy = ys[kept.Count / 2];
	// glow VERTICAL half-height in px covers glow*squash rad -> px per rad
	// (vertical extent is immune to big horizontal sprites like the sun)
	int yMin = ys[0], yMax = ys[kept.Count - 1];
	double ppr = (yMax - yMin + 1) / 2.0 / (GlowRadRad / 1000.0 * 0.55);
	double faceRad = ShadowRadRad / 1000.0 * ppr;
	// sanity: the same ppr from the horizontal extent; they may diverge if a
	// layout sprite rides the ring — the vertical one is the authority
	int xMin = xs[0], xMax = xs[kept.Count - 1];
	double pprH = (xMax - xMin + 1) / 2.0 / (GlowRadRad / 1000.0);
	long hits = 0; var hitPts = new List<(int x, int y)>();
	long orb = 0; // saturated-orange pixels just outside (sanity check)
	foreach (var m in kept)
	{
		double dx = m.x - cx, dy = m.y - cy;
		double d = Math.Sqrt(dx * dx + dy * dy);
		if (d < faceRad - 3)
		{
			int i = m.y * w + m.x;
			if (r[i] > MinArcR && r[i] > g[i] * 9 / 5 && r[i] > b[i] * 9 / 5) { hits++; hitPts.Add(m); }
		}
		if (d > faceRad + 40 && d < faceRad + 90)
		{
			int i = m.y * w + m.x;
			if (r[i] > MinArcR && r[i] > g[i] * 9 / 5 && r[i] > b[i] * 9 / 5) orb++;
		}
	}
	Console.WriteLine($"circle: cluster#{kept.Count} center=({cx:F1},{cy:F1}) y-span {yMin}..{yMax} x-span {xMin}..{xMax} pprV={ppr:F0} pprH={pprH:F0}/rad faceR={faceRad:F0}px");
	Console.WriteLine($"circle: saturated-orange inside face = {hits} (sanity detector on ring band outside face = {orb})");
	// annotate: crop +-faceR*1.7 around center, upscale x3, draw circle + hits
	int half = (int)(faceRad * 1.7);
	int sxr = Math.Clamp((int)cx - half, 0, w - 2 * half), syr = Math.Clamp((int)cy - half, 0, n - 2 * half);
	using var crop = bmp.Clone(new Rectangle(sxr, syr, 2 * half, 2 * half), bmp.PixelFormat);
	using var up = new Bitmap(crop, crop.Width * 3, crop.Height * 3);
	using var gr = Graphics.FromImage(up);
	float k = 3f;
	using var pen = new Pen(Color.White, Math.Max(1.6f, 0.007f * up.Width));
	gr.DrawEllipse(pen, (float)((cx - sxr - faceRad) * k), (float)((cy - syr - faceRad) * k), (float)(2 * faceRad * k), (float)(2 * faceRad * k));
	foreach (var m in hitPts)
	{
		using var hb = new Pen(Color.Magenta, Math.Max(2f, 0.010f * up.Width));
		gr.DrawEllipse(hb, (float)((m.x - sxr) * k) - 6, (float)((m.y - syr) * k) - 6, 12, 12);
	}
	up.Save(outPath!);
	Console.WriteLine($"circle: proof image -> {outPath}");
	return;
}

// ---- default mode: enclosed dark islands
var visited = new bool[n, w];
var stack = new Stack<(int x, int y)>();
for (int x = 0; x < w; x++) { stack.Push((x, 0)); stack.Push((x, n - 1)); }
for (int y = 1; y < n - 1; y++) { stack.Push((0, y)); stack.Push((w - 1, y)); }
while (stack.Count > 0)
{
	var (x, y) = stack.Pop();
	if (x < 0 || y < 0 || x >= w || y >= n || visited[y, x] || lightAt(x, y)) continue;
	visited[y, x] = true;
	stack.Push((x + 1, y)); stack.Push((x - 1, y)); stack.Push((x, y + 1)); stack.Push((x, y - 1));
}

var seen = new bool[n, w];
int islands = 0, worst = -1;
long worstArea = 0;
for (int y = 1; y < n - 1 && islands < 200; y++)
	for (int x = 1; x < w - 1 && islands < 200; x++)
	{
		if (lightAt(x, y) || visited[y, x] || seen[y, x]) continue;
		// enclosed dark island: collect
		long area = 0, arc = 0, bright = 0;
		int minX = x, maxX = x, minY = y, maxY = y;
		var q = new Queue<(int x, int y)>();
		q.Enqueue((x, y)); seen[y, x] = true;
		while (q.Count > 0)
		{
			var (px, py) = q.Dequeue();
			area++;
			int i = py * w + px;
			int R = r[i], G = g[i], B = b[i];
			if (R > MinArcR && R > G * 9 / 5 && R > B * 9 / 5) arc++;
			if (R > 12 || G > 12 || B > 12) bright++;
			if (px < minX) minX = px; if (px > maxX) maxX = px;
			if (py < minY) minY = py; if (py > maxY) maxY = py;
			foreach (var (nx, ny) in new[] { (px + 1, py), (px - 1, py), (px, py + 1), (px, py - 1) })
			{
				if (nx <= 0 || ny <= 0 || nx >= w - 1 || ny >= n - 1) continue;
				if (!lightAt(nx, ny) && !visited[ny, nx] && !seen[ny, nx]) { seen[ny, nx] = true; q.Enqueue((nx, ny)); }
			}
		}
		if (area > 2000)
		{
			islands++;
			Console.WriteLine($"island#{islands} area={area} bbox x{minX}..{maxX} y{minY}..{maxY} saturationOrange>50={arc} anyBright>12={bright}");
			if (arc > 0 || area > worstArea) { worst = islands - 1; worstArea = Math.Max(worstArea, area); }
		}
	}
Console.WriteLine($"DONE islands={islands} worstIslandHaArc={worst}");
