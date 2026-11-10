using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>Tier-framed ship cell with hull and shield bars; empty when <c>ship</c> is null.</summary>
public sealed partial class ShipBox : HudTooltipHost
{
	public static readonly Color HullColor = new(0.85f, 0.15f, 0.18f);
	public static readonly Color ShieldColor = new(0.25f, 0.62f, 0.95f);

	public ShipBox(ShipInstance? ship, float size)
	{
		CustomMinimumSize = Vector2.One * size;
		MouseFilter = ship is null ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
		AddChild(Frame(Vector2.Zero, size, ship is null ? HudPalettes.BlueCyan.Border : TierColor(ship)));
		if (ship is null)
			return;

		var inset = size * 0.125f;
		var barHeight = Mathf.Max(2f, size / 16f);
		AddChild(Portrait(ship, new Vector2(inset, inset + barHeight * 2f), Vector2.One * (size - inset * 2f - barHeight * 2f)));
		var barWidth = size - inset * 2f;
		AddChild(Bar(ShieldColor, ship.TotalCurrentShieldPoints, ship.TotalBaseShieldPoints,
			new Vector2(inset, inset * 0.6f), new Vector2(barWidth, barHeight)));
		AddChild(Bar(HullColor, ship.CurrentHullPoints, ship.Loadout.BaseHullPoints,
			new Vector2(inset, inset * 0.6f + barHeight + 1f), new Vector2(barWidth, barHeight)));
		MouseDefaultCursorShape = CursorShape.PointingHand;
	}

	public static Color TierColor(ShipInstance ship) =>
		ShipGearTierTint.TryGet(ShipLoadoutGearTier.FromLoadout(ship.Loadout, ship.Spec.Chassis), out var tint)
			? tint
			: Colors.Gray;

	public static TextureRect Frame(Vector2 position, float size, Color edge)
	{
		var frame = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = GD.Load<Texture2D>("res://assets/ui/components/runtime/deployment_pilot_frame_blue.png")
				?? throw new InvalidOperationException("Missing deployment frame."),
			Position = position,
			Size = Vector2.One * size,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://assets/ui/components/runtime/button_palette.gdshader")
				?? throw new InvalidOperationException("Missing UI palette shader."),
		};
		material.SetShaderParameter("surface_color", HudPalettes.BlueCyan.DeepSurface);
		material.SetShaderParameter("edge_color", edge);
		material.SetShaderParameter("palette_mix", 1f);
		frame.Material = material;
		return frame;
	}

	public static ProgressBar CreateBar(Color color, Vector2 minimumSize)
	{
		var bar = new ProgressBar
		{
			CustomMinimumSize = minimumSize,
			ShowPercentage = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		bar.AddThemeStyleboxOverride("fill", BarStyle(color));
		bar.AddThemeStyleboxOverride("background",
			BarStyle(HudPalettes.WithAlpha(HudPalettes.BlueCyan.Surface, 0.95f)));
		return bar;
	}

	public static void SetBar(ProgressBar bar, int current, int max)
	{
		max = Mathf.Max(0, max);
		bar.MaxValue = max;
		bar.Value = Mathf.Clamp(current, 0, max);
	}

	private static ProgressBar Bar(Color color, int current, int max, Vector2 position, Vector2 size)
	{
		var bar = CreateBar(color, size);
		bar.Position = position;
		bar.Size = size;
		SetBar(bar, current, max);
		return bar;
	}

	private static TextureRect Portrait(ShipInstance ship, Vector2 position, Vector2 size) =>
		new()
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = UnitPortraitCatalog.For(ship.Spec.Chassis)
				?? throw new InvalidOperationException("Missing ship portrait."),
			Position = position,
			Size = size,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};

	private static StyleBoxFlat BarStyle(Color color) =>
		new()
		{
			BgColor = color,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
		};
}
