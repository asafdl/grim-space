using Godot;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ShipPilot : PanelContainer
{
	private const int PortraitSize = 88;
	private const int BarWidth = 88;

	private TextureRect _portrait = null!;
	private ProgressBar _hullBar = null!;
	private ProgressBar _shieldBar = null!;
	private string? _portraitId;

	public override void _Ready()
	{
		Build();
	}

	public void SetState(string portraitId, ShipInstance ship)
	{
		if (!string.Equals(_portraitId, portraitId, StringComparison.Ordinal))
		{
			_portrait.Texture = ShipPilotPortraitCatalog.TextureFor(portraitId);
			_portraitId = portraitId;
		}
		SetBar(_hullBar, ship.HullPoints, ship.Loadout.MaxHullPoints);
		SetBar(_shieldBar, ship.TotalCurrentShieldPoints, ship.TotalMaxShieldPoints);
		_portrait.TooltipText =
			$"{ship.Spec.Chassis} Pilot\n"
			+ $"HP {ship.HullPoints}/{ship.Loadout.MaxHullPoints}\n"
			+ $"SP {ship.TotalCurrentShieldPoints}/{ship.TotalMaxShieldPoints}";
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		AddThemeStyleboxOverride(
			"panel",
			new StyleBoxFlat
			{
				BgColor = new Color(0.035f, 0.05f, 0.08f, 0.9f),
				BorderColor = new Color(0.3f, 0.42f, 0.6f, 0.8f),
				BorderWidthLeft = 1,
				BorderWidthTop = 1,
				BorderWidthRight = 1,
				BorderWidthBottom = 1,
				CornerRadiusTopLeft = 4,
				CornerRadiusTopRight = 4,
				CornerRadiusBottomRight = 4,
				CornerRadiusBottomLeft = 4,
			});

		var margin = new MarginContainer();
		foreach (var marginName in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
			margin.AddThemeConstantOverride(marginName, 8);
		AddChild(margin);

		var column = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddThemeConstantOverride("separation", 5);
		margin.AddChild(column);

		_portrait = new TextureRect
		{
			CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Stop,
		};
		column.AddChild(_portrait);
		_hullBar = CreateBar(new Color(0.85f, 0.15f, 0.18f));
		_shieldBar = CreateBar(new Color(0.25f, 0.62f, 0.95f));
		column.AddChild(_hullBar);
		column.AddChild(_shieldBar);
	}

	private static ProgressBar CreateBar(Color color)
	{
		var bar = new ProgressBar
		{
			CustomMinimumSize = new Vector2(BarWidth, 6),
			ShowPercentage = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		bar.AddThemeStyleboxOverride("fill", MakeBarStyle(color));
		bar.AddThemeStyleboxOverride("background", MakeBarStyle(new Color(0.08f, 0.1f, 0.14f, 0.95f)));
		return bar;
	}

	private static void SetBar(ProgressBar bar, int current, int max)
	{
		max = Mathf.Max(0, max);
		current = Mathf.Clamp(current, 0, max);
		bar.MaxValue = max;
		bar.Value = current;
	}

	private static StyleBoxFlat MakeBarStyle(Color color) =>
		new()
		{
			BgColor = color,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
		};
}
