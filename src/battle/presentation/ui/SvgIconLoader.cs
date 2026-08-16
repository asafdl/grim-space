using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class SvgIconLoader
{
	public static Texture2D Load(string? path, Color tint, int sizePx)
	{
		var empty = ImageTexture.CreateFromImage(Image.CreateEmpty(sizePx, sizePx, false, Image.Format.Rgba8));
		if (path is null || !ResourceLoader.Exists(path))
		{
			if (path is not null)
				GD.PushWarning($"SvgIconLoader: icon not found '{path}'");
			return empty;
		}

		var texture = ResourceLoader.Load<Texture2D>(path);
		if (texture is null)
		{
			GD.PushWarning($"SvgIconLoader: failed to load icon '{path}'");
			return empty;
		}

		var image = texture.GetImage();
		if (image.IsEmpty())
			return empty;

		if (image.GetWidth() != sizePx || image.GetHeight() != sizePx)
			image.Resize(sizePx, sizePx);

		Tint(image, tint);
		return ImageTexture.CreateFromImage(image);
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
