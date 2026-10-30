using System.Linq;
using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class PilotLevelUpChevron : Control
{
	private const float DefaultSize = 28f;

	private float _pulseTime;

	public bool Pulse { get; set; } = true;

	public PilotLevelUpChevron()
	{
		CustomMinimumSize = Vector2.One * DefaultSize;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		if (!Pulse || !Visible)
		{
			Scale = Vector2.One;
			Modulate = Colors.White;
			return;
		}

		_pulseTime += (float)delta;
		var pulse = (Mathf.Sin(_pulseTime * Mathf.Tau / 1.35f) + 1f) * 0.5f;
		PivotOffset = Size * 0.5f;
		Scale = Vector2.One * Mathf.Lerp(0.94f, 1.08f, pulse);
		Modulate = new Color(1f, 1f, 1f, Mathf.Lerp(0.78f, 1f, pulse));
	}

	public override void _Draw()
	{
		var scale = Size.X / DefaultSize;
		var top = new[] { V(4f, 9f, scale), V(14f, 2f, scale), V(24f, 9f, scale) };
		var middle = new[] { V(4f, 16f, scale), V(14f, 9f, scale), V(24f, 16f, scale) };
		var arrow = new[]
		{
			V(14f, 13f, scale),
			V(26f, 22f, scale),
			V(20f, 22f, scale),
			V(20f, 29f, scale),
			V(8f, 29f, scale),
			V(8f, 22f, scale),
			V(2f, 22f, scale),
		};

		var shadow = HudPalettes.WithAlpha(HudPalettes.PurpleRed.DeepSurface, 0.9f);
		var lavender = HudPalettes.PurpleRed.Primary;
		var purple = HudPalettes.PurpleRed.Secondary;
		var crimson = HudPalettes.PurpleRed.Highlight;

		DrawChevron(top, shadow, 7f * scale);
		DrawChevron(top, lavender, 4f * scale);
		DrawChevron(middle, shadow, 7f * scale);
		DrawChevron(middle, purple, 4f * scale);
		DrawPolygon(arrow, Enumerable.Repeat(shadow, arrow.Length).ToArray());
		DrawPolyline([.. arrow, arrow[0]], shadow, 3f * scale, true);

		var insetArrow = new[]
		{
			V(14f, 15.5f, scale),
			V(23f, 21.5f, scale),
			V(18f, 21.5f, scale),
			V(18f, 27f, scale),
			V(10f, 27f, scale),
			V(10f, 21.5f, scale),
			V(5f, 21.5f, scale),
		};
		DrawPolygon(insetArrow, Enumerable.Repeat(crimson, insetArrow.Length).ToArray());
	}

	private static Vector2 V(float x, float y, float scale) => new(x * scale, y * scale);

	private void DrawChevron(Vector2[] points, Color color, float width)
	{
		DrawLine(points[0], points[1], color, width, true);
		DrawLine(points[1], points[2], color, width, true);
	}
}
