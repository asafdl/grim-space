using Godot;

namespace GrimSpace.Components;

public static class SvgIconLoader
{
	public static Texture2D Load(string? path, Color tint, int sizePx) =>
		Load(path, sizePx, tintPixels: true, tint);

	public static Texture2D LoadRaw(string? path, int sizePx) =>
		Load(path, sizePx, tintPixels: false, Colors.White);

	public static Texture2D LoadCentered(string? path, Color tint, int sizePx, int paddingPx = 3)
	{
		var image = LoadCenteredImage(path, sizePx, paddingPx);
		Tint(image, tint);
		return ImageTexture.CreateFromImage(image);
	}

	public static Texture2D LoadRawCentered(string? path, int sizePx, int paddingPx = 3) =>
		ImageTexture.CreateFromImage(LoadCenteredImage(path, sizePx, paddingPx));

	private static Texture2D Load(string? path, int sizePx, bool tintPixels, Color tint)
	{
		var image = LoadImage(path, sizePx);
		if (tintPixels)
			Tint(image, tint);
		return ImageTexture.CreateFromImage(image);
	}

	private static Image LoadCenteredImage(string? path, int sizePx, int paddingPx)
	{
		var image = LoadImage(path, sizePx);
		return CenterOpaquePixels(image, sizePx, paddingPx);
	}

	private static Image LoadImage(string? path, int sizePx)
	{
		var empty = ImageTexture.CreateFromImage(Image.CreateEmpty(sizePx, sizePx, false, Image.Format.Rgba8));
		if (path is null || !ResourceLoader.Exists(path))
		{
			if (path is not null)
				GD.PushWarning($"SvgIconLoader: icon not found '{path}'");
			return empty.GetImage();
		}

		var texture = ResourceLoader.Load<Texture2D>(path);
		if (texture is null)
		{
			GD.PushWarning($"SvgIconLoader: failed to load icon '{path}'");
			return empty.GetImage();
		}

		var image = texture.GetImage();
		if (image.IsEmpty())
			return empty.GetImage();

		if (image.GetWidth() != sizePx || image.GetHeight() != sizePx)
			image.Resize(sizePx, sizePx);
		return image;
	}

	private static Image CenterOpaquePixels(Image image, int sizePx, int paddingPx)
	{
		var minX = image.GetWidth();
		var minY = image.GetHeight();
		var maxX = -1;
		var maxY = -1;
		for (var y = 0; y < image.GetHeight(); y++)
		for (var x = 0; x < image.GetWidth(); x++)
		{
			if (image.GetPixel(x, y).A <= 0.001f)
				continue;
			minX = System.Math.Min(minX, x);
			minY = System.Math.Min(minY, y);
			maxX = System.Math.Max(maxX, x);
			maxY = System.Math.Max(maxY, y);
		}

		if (maxX < minX || maxY < minY)
			return image;

		var sourceWidth = maxX - minX + 1;
		var sourceHeight = maxY - minY + 1;
		var available = System.Math.Max(1, sizePx - paddingPx * 2);
		var scale = System.Math.Min(
			available / (float)sourceWidth,
			available / (float)sourceHeight);
		var targetWidth = System.Math.Max(1, Mathf.RoundToInt(sourceWidth * scale));
		var targetHeight = System.Math.Max(1, Mathf.RoundToInt(sourceHeight * scale));
		var glyph = image.GetRegion(new Rect2I(minX, minY, sourceWidth, sourceHeight));
		glyph.Resize(targetWidth, targetHeight);
		var (centerX, centerY) = OpaqueCenter(glyph);
		var destinationX = Mathf.Clamp(
			Mathf.RoundToInt(sizePx * 0.5f - centerX),
			0,
			sizePx - targetWidth);
		var destinationY = Mathf.Clamp(
			Mathf.RoundToInt(sizePx * 0.5f - centerY),
			0,
			sizePx - targetHeight);

		var centered = Image.CreateEmpty(sizePx, sizePx, false, Image.Format.Rgba8);
		centered.BlitRect(
			glyph,
			new Rect2I(0, 0, targetWidth, targetHeight),
			new Vector2I(destinationX, destinationY));
		return centered;
	}

	private static (float X, float Y) OpaqueCenter(Image image)
	{
		var weightedX = 0f;
		var weightedY = 0f;
		var totalAlpha = 0f;
		for (var y = 0; y < image.GetHeight(); y++)
		for (var x = 0; x < image.GetWidth(); x++)
		{
			var alpha = image.GetPixel(x, y).A;
			weightedX += (x + 0.5f) * alpha;
			weightedY += (y + 0.5f) * alpha;
			totalAlpha += alpha;
		}

		return totalAlpha > 0f
			? (weightedX / totalAlpha, weightedY / totalAlpha)
			: (image.GetWidth() * 0.5f, image.GetHeight() * 0.5f);
	}

	private static void Tint(Image image, Color tint)
	{
		var w = image.GetWidth();
		var h = image.GetHeight();
		for (var y = 0; y < h; y++)
		for (var x = 0; x < w; x++)
		{
			var p = image.GetPixel(x, y);
			if (p.A <= 0.001f)
				continue;
			image.SetPixel(x, y, new Color(tint.R, tint.G, tint.B, p.A * tint.A));
		}
	}
}
