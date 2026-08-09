using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Compact AP blocks shown above the action bar.</summary>
public sealed partial class ApBar : HBoxContainer
{
	private const float BlockWidth = 36f;
	private const float BlockHeight = 12f;

	private static readonly Color Filled = new(0.28f, 0.85f, 0.4f, 0.95f);
	private static readonly Color Empty = new(0.12f, 0.16f, 0.14f, 0.85f);
	private static readonly Color BorderFilled = new(0.45f, 1f, 0.55f, 0.9f);
	private static readonly Color BorderEmpty = new(0.28f, 0.35f, 0.3f, 0.7f);

	private readonly List<Panel> _blocks = [];

	public ApBar()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Alignment = AlignmentMode.Center;
		AddThemeConstantOverride("separation", 4);
	}

	public void Set(int current, int max)
	{
		max = System.Math.Max(0, max);
		current = System.Math.Clamp(current, 0, max);

		while (_blocks.Count < max)
		{
			var block = new Panel
			{
				CustomMinimumSize = new Vector2(BlockWidth, BlockHeight),
				MouseFilter = MouseFilterEnum.Ignore,
			};
			_blocks.Add(block);
			AddChild(block);
		}

		while (_blocks.Count > max)
		{
			var last = _blocks[^1];
			_blocks.RemoveAt(_blocks.Count - 1);
			last.QueueFree();
		}

		for (var i = 0; i < _blocks.Count; i++)
		{
			var filled = i < current;
			_blocks[i].AddThemeStyleboxOverride(
				"panel",
				MakeStyle(filled ? Filled : Empty, filled ? BorderFilled : BorderEmpty));
		}
	}

	private static StyleBoxFlat MakeStyle(Color bg, Color border) =>
		new()
		{
			BgColor = bg,
			BorderColor = border,
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomRight = 2,
			CornerRadiusBottomLeft = 2,
		};
}
