using Godot;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>Split ring around a circular ship slot: shields fill the left half, hull the right, both bottom-up.</summary>
public sealed partial class ShipHealthArcs : Control
{
	private const float Width = 4f;
	private const float Gap = 0.18f;
	private static readonly Color Track = new(0f, 0f, 0f, 0.55f);

	private readonly float _shield;
	private readonly float _hull;

	public ShipHealthArcs(ShipInstance ship)
	{
		_shield = Fraction(ship.TotalCurrentShieldPoints, ship.TotalBaseShieldPoints);
		_hull = Fraction(ship.CurrentHullPoints, ship.Loadout.BaseHullPoints);
		MouseFilter = MouseFilterEnum.Ignore;
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
	}

	public override void _Draw()
	{
		var center = Size / 2f;
		var radius = Mathf.Min(Size.X, Size.Y) / 2f - Width / 2f;
		var sweep = Mathf.Pi - 2f * Gap;
		var bottom = Mathf.Pi / 2f;
		Half(center, radius, bottom + Gap, sweep, _shield, ShipBox.ShieldColor);
		Half(center, radius, bottom - Gap, -sweep, _hull, ShipBox.HullColor);
	}

	private void Half(Vector2 center, float radius, float start, float sweep, float fill, Color color)
	{
		DrawArc(center, radius, start, start + sweep, 24, Track, Width + 2f, true);
		if (fill > 0f)
			DrawArc(center, radius, start, start + sweep * fill, 24, color, Width, true);
	}

	private static float Fraction(float current, float max) =>
		max <= 0f ? 0f : Mathf.Clamp(current / max, 0f, 1f);
}
