using Godot;

namespace GrimSpace.Components;

public enum ShieldBlockBarSize
{
	Default,
	Compact,
}

public readonly record struct ShieldBlockMetrics(float Width, float Height, int Separation)
{
	public static ShieldBlockMetrics For(ShieldBlockBarSize size) =>
		size switch
		{
			ShieldBlockBarSize.Compact => new(14f, 6f, 2),
			_ => new(28f, 11f, 2),
		};
}

public static class ShieldBlockVisuals
{
	public static readonly Color Filled = new(0.25f, 0.55f, 0.95f, 0.95f);
	public static readonly Color Empty = new(0.08f, 0.12f, 0.22f, 0.85f);
	public static readonly Color Border = new(0.92f, 0.95f, 1f, 0.95f);

	public static StyleBoxFlat Style(bool filled) =>
		MakeStyle(filled ? Filled : Empty, Border, borderWidth: 2);

	public static void SyncBlocks(
		Container host,
		List<Panel> blocks,
		ShieldBlockMetrics metrics,
		int count,
		int filledCount)
	{
		count = System.Math.Max(0, count);
		filledCount = System.Math.Clamp(filledCount, 0, count);

		while (blocks.Count < count)
		{
			var block = new Panel
			{
				CustomMinimumSize = new Vector2(metrics.Width, metrics.Height),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			blocks.Add(block);
			host.AddChild(block);
		}

		while (blocks.Count > count)
		{
			var last = blocks[^1];
			blocks.RemoveAt(blocks.Count - 1);
			last.QueueFree();
		}

		for (var i = 0; i < blocks.Count; i++)
			blocks[i].AddThemeStyleboxOverride("panel", Style(i < filledCount));
	}

	private static StyleBoxFlat MakeStyle(Color bg, Color border, int borderWidth) =>
		new()
		{
			BgColor = bg,
			BorderColor = border,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomRight = 2,
			CornerRadiusBottomLeft = 2,
		};
}
