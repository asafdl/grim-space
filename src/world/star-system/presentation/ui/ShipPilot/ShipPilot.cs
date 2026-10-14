using Godot;
using GrimSpace.Run;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ShipPilot : PanelContainer
{
	private const int PortraitSize = 64;
	private const int BarWidth = 64;

	private static readonly Color DefaultBorderColor = new(0.3f, 0.42f, 0.6f, 0.8f);
	private static readonly Color SelectedBorderColor = new(0.55f, 0.78f, 1f, 1f);

	private Label _levelLabel = null!;
	private Button _levelUpButton = null!;
	private TextureRect _portrait = null!;
	private ProgressBar _hullBar = null!;
	private string? _portraitId;
	private string? _pilotId;
	private string? _shipId;
	private bool _selected;

	public event Action<string>? Selected;
	public event Action<string>? PilotSheetRequested;

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
		_levelLabel.Text = $"LV {claimedLevel}";
		_levelUpButton.Visible = canLearnSkill;
		SetBar(_hullBar, ship.HullPoints, ship.Loadout.MaxHullPoints);
		var (xpInLevel, xpToNext) = PilotProgression.ProgressWithinLevel(experiencePoints);
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
			margin.AddThemeConstantOverride(marginName, 4);
		AddChild(margin);

		var column = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddThemeConstantOverride("separation", 3);
		margin.AddChild(column);

		var portraitFrame = new Control
		{
			CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddChild(portraitFrame);

		_portrait = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			MouseFilter = MouseFilterEnum.Stop,
		};
		_portrait.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_portrait.GuiInput += OnPortraitGuiInput;
		portraitFrame.AddChild(_portrait);

		_levelLabel = new Label
		{
			OffsetLeft = 1,
			OffsetTop = PortraitSize - 17,
			OffsetRight = 29,
			OffsetBottom = PortraitSize - 1,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_levelLabel.AddThemeFontSizeOverride("font_size", 10);
		_levelLabel.AddThemeColorOverride("font_color", Colors.White);
		portraitFrame.AddChild(_levelLabel);

		_levelUpButton = new Button
		{
			Text = "↑",
			TooltipText = "Choose a pilot skill",
			OffsetLeft = PortraitSize - 23,
			OffsetTop = PortraitSize - 23,
			OffsetRight = PortraitSize - 3,
			OffsetBottom = PortraitSize - 3,
			Visible = false,
		};
		_levelUpButton.AddThemeFontSizeOverride("font_size", 14);
		_levelUpButton.AddThemeColorOverride("font_color", Colors.White);
		_levelUpButton.AddThemeStyleboxOverride("normal", BadgeStyle(new Color(0.18f, 0.42f, 0.7f, 0.95f)));
		_levelUpButton.AddThemeStyleboxOverride("hover", BadgeStyle(new Color(0.3f, 0.58f, 0.9f, 1f)));
		_levelUpButton.AddThemeStyleboxOverride("pressed", BadgeStyle(new Color(0.12f, 0.32f, 0.58f, 1f)));
		_levelUpButton.Pressed += OnLevelUpPressed;
		portraitFrame.AddChild(_levelUpButton);

		_hullBar = CreateBar(new Color(0.85f, 0.15f, 0.18f));
		column.AddChild(_hullBar);
	}

	private void OnPortraitGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true } mouseButton)
			return;

		if (mouseButton.ButtonIndex == MouseButton.Left && _shipId is not null)
			Selected?.Invoke(_shipId);
		else if (mouseButton.ButtonIndex == MouseButton.Right && _pilotId is not null)
			PilotSheetRequested?.Invoke(_pilotId);
	}

	private void OnLevelUpPressed()
	{
		if (_pilotId is not null)
			PilotSheetRequested?.Invoke(_pilotId);
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

	private static StyleBoxFlat BadgeStyle(Color color) =>
		new()
		{
			BgColor = color,
			CornerRadiusTopLeft = 3,
			CornerRadiusTopRight = 3,
			CornerRadiusBottomLeft = 3,
			CornerRadiusBottomRight = 3,
			ContentMarginLeft = 2,
			ContentMarginRight = 2,
		};
}
