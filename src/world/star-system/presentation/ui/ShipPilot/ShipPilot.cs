using Godot;
using GrimSpace.Run;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ShipPilot : PanelContainer
{
	private const int PortraitSize = 88;
	private const int BarWidth = 88;

	private static readonly Color DefaultBorderColor = new(0.3f, 0.42f, 0.6f, 0.8f);
	private static readonly Color SelectedBorderColor = new(0.55f, 0.78f, 1f, 1f);

	private Label _levelLabel = null!;
	private Button _levelUpButton = null!;
	private TextureRect _portrait = null!;
	private ProgressBar _hullBar = null!;
	private ProgressBar _shieldBar = null!;
	private ProgressBar _xpBar = null!;
	private string? _portraitId;
	private string? _pilotId;
	private string? _shipId;
	private bool _selected;

	public event Action<string>? Selected;
	public event Action<string>? LevelUpRequested;

	public override void _Ready()
	{
		Build();
	}

	public void SetState(
		string pilotId,
		string pilotName,
		string portraitId,
		int experiencePoints,
		int claimedLevel,
		bool canLearnSkill,
		ShipInstance ship,
		bool selected)
	{
		_pilotId = pilotId;
		_shipId = ship.Id;
		if (!string.Equals(_portraitId, portraitId, StringComparison.Ordinal))
		{
			_portrait.Texture = ShipPilotPortraitCatalog.TextureFor(portraitId);
			_portraitId = portraitId;
		}
		_levelLabel.Text = $"Lv {claimedLevel}";
		_levelUpButton.Visible = canLearnSkill;
		SetBar(_hullBar, ship.HullPoints, ship.Loadout.MaxHullPoints);
		SetBar(_shieldBar, ship.TotalCurrentShieldPoints, ship.TotalMaxShieldPoints);
		var (xpInLevel, xpToNext) = PilotProgression.ProgressWithinLevel(experiencePoints);
		SetBar(
			_xpBar,
			canLearnSkill ? 1 : xpInLevel,
			canLearnSkill ? 1 : xpToNext > 0 ? xpToNext : 1);
		var xpLine = xpToNext > 0
			? $"XP {xpInLevel}/{xpToNext}"
			: "XP max";
		_portrait.TooltipText =
			$"{pilotName}\n"
			+ $"Lv {claimedLevel} · {xpLine}\n"
			+ (canLearnSkill ? "Level up available\n" : "")
			+ $"{ship.Spec.Chassis}\n"
			+ $"HP {ship.HullPoints}/{ship.Loadout.MaxHullPoints}\n"
			+ $"SP {ship.TotalCurrentShieldPoints}/{ship.TotalMaxShieldPoints}";
		SetSelected(selected);
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ApplyPanelStyle(selected: false);

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

		var levelRow = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddChild(levelRow);

		_levelLabel = new Label
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_levelLabel.AddThemeFontSizeOverride("font_size", 11);
		_levelLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.82f, 0.92f));
		levelRow.AddChild(_levelLabel);

		_levelUpButton = new Button
		{
			Text = "↑",
			TooltipText = "Choose a pilot skill",
			CustomMinimumSize = new Vector2(24, 24),
			Visible = false,
		};
		_levelUpButton.AddThemeFontSizeOverride("font_size", 15);
		_levelUpButton.Pressed += OnLevelUpPressed;
		levelRow.AddChild(_levelUpButton);

		_portrait = new TextureRect
		{
			CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Stop,
		};
		_portrait.GuiInput += OnPortraitGuiInput;
		column.AddChild(_portrait);
		_hullBar = CreateBar(new Color(0.85f, 0.15f, 0.18f));
		_shieldBar = CreateBar(new Color(0.25f, 0.62f, 0.95f));
		_xpBar = CreateBar(new Color(0.62f, 0.38f, 0.92f));
		column.AddChild(_hullBar);
		column.AddChild(_shieldBar);
		column.AddChild(_xpBar);
	}

	private void OnPortraitGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
			return;
		if (_shipId is null)
			return;

		Selected?.Invoke(_shipId);
	}

	private void OnLevelUpPressed()
	{
		if (_pilotId is not null)
			LevelUpRequested?.Invoke(_pilotId);
	}

	private void SetSelected(bool selected)
	{
		if (_selected == selected)
			return;

		_selected = selected;
		ApplyPanelStyle(selected);
	}

	private void ApplyPanelStyle(bool selected)
	{
		var borderColor = selected ? SelectedBorderColor : DefaultBorderColor;
		var borderWidth = selected ? 2 : 1;
		AddThemeStyleboxOverride(
			"panel",
			new StyleBoxFlat
			{
				BgColor = new Color(0.035f, 0.05f, 0.08f, 0.9f),
				BorderColor = borderColor,
				BorderWidthLeft = borderWidth,
				BorderWidthTop = borderWidth,
				BorderWidthRight = borderWidth,
				BorderWidthBottom = borderWidth,
				CornerRadiusTopLeft = 4,
				CornerRadiusTopRight = 4,
				CornerRadiusBottomRight = 4,
				CornerRadiusBottomLeft = 4,
			});
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
