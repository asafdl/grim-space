using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ShipPilot : PanelContainer
{
	private const int PortraitSize = 64;
	private static readonly Vector2 BarSize = new(64, 6);

	private static readonly Color BorderColor =
		HudPalettes.WithAlpha(HudPalettes.BlueCyan.Border, 0.8f);

	private Label _levelLabel = null!;
	private LevelUpButton _levelUpButton = null!;
	private TextureRect _portrait = null!;
	private ProgressBar _shieldBar = null!;
	private ProgressBar _hullBar = null!;
	private ProgressBar _xpBar = null!;
	private string? _portraitId;
	private string? _pilotId;

	public event Action<string>? PilotSheetRequested;

	public Control Portrait => _portrait;

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
		ShipInstance? ship)
	{
		_pilotId = pilotId;
		if (!string.Equals(_portraitId, portraitId, StringComparison.Ordinal))
		{
			_portrait.Texture = ShipPilotPortraitCatalog.TextureFor(portraitId);
			_portraitId = portraitId;
		}
		_levelLabel.Text = $"LV {claimedLevel}";
		_levelUpButton.Visible = canLearnSkill;
		_shieldBar.Visible = ship is not null;
		_hullBar.Visible = ship is not null;
		if (ship is not null)
		{
			ShipBox.SetBar(_shieldBar, ship.TotalCurrentShieldPoints, ship.TotalBaseShieldPoints);
			ShipBox.SetBar(_hullBar, ship.CurrentHullPoints, ship.Loadout.BaseHullPoints);
		}
		var (xpInLevel, xpToNext) = PilotProgression.ProgressWithinLevel(experiencePoints);
		if (xpToNext > 0)
			ShipBox.SetBar(_xpBar, xpInLevel, xpToNext);
		else
		{
			_xpBar.MaxValue = 1;
			_xpBar.Value = 1;
		}
		var xpLine = xpToNext > 0
			? $"XP {xpInLevel}/{xpToNext}"
			: "XP max";
		var shipDetails = ship is null
			? "No ship - cannot deploy"
			: $"{ship.Spec.Chassis}\n"
				+ $"HP {ship.CurrentHullPoints}/{ship.Loadout.BaseHullPoints}\n"
				+ $"SP {ship.TotalCurrentShieldPoints}/{ship.TotalBaseShieldPoints}";
		HudTooltipCard.SetTooltip(
			_portrait,
			$"{pilotName}\n"
			+ $"Lv {claimedLevel} · {xpLine}\n"
			+ (canLearnSkill ? "Level up available\n" : "")
			+ shipDetails);
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(PortraitSize + 8, 0);
		SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		ApplyPanelStyle();

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
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
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

		_levelUpButton = new LevelUpButton
		{
			OffsetLeft = PortraitSize - 29,
			OffsetTop = PortraitSize - 31,
			OffsetRight = PortraitSize - 1,
			OffsetBottom = PortraitSize - 1,
			Visible = false,
		};
		HudTooltipCard.SetTooltip(_levelUpButton, "Level up");
		_levelUpButton.Pressed += OnLevelUpPressed;
		portraitFrame.AddChild(_levelUpButton);

		_shieldBar = ShipBox.CreateBar(ShipBox.ShieldColor, BarSize);
		column.AddChild(_shieldBar);
		_hullBar = ShipBox.CreateBar(ShipBox.HullColor, BarSize);
		column.AddChild(_hullBar);
		_xpBar = ShipBox.CreateBar(HudPalettes.PurpleRed.Secondary, BarSize);
		column.AddChild(_xpBar);
	}

	private void OnPortraitGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }
			&& _pilotId is not null)
			PilotSheetRequested?.Invoke(_pilotId);
	}

	private void OnLevelUpPressed()
	{
		if (_pilotId is not null)
			PilotSheetRequested?.Invoke(_pilotId);
	}

	private void ApplyPanelStyle()
	{
		const int borderWidth = 1;
		AddThemeStyleboxOverride(
			"panel",
			new StyleBoxFlat
			{
				BgColor = HudPalettes.WithAlpha(HudPalettes.BlueCyan.Surface, 0.9f),
				BorderColor = BorderColor,
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

	private sealed partial class LevelUpButton : Button
	{
		public LevelUpButton()
		{
			FocusMode = FocusModeEnum.None;
			MouseDefaultCursorShape = CursorShape.PointingHand;
			foreach (var state in new[] { "normal", "hover", "pressed", "focus" })
				AddThemeStyleboxOverride(state, new StyleBoxEmpty());

			var chevron = new PilotLevelUpChevron();
			chevron.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			chevron.GrowHorizontal = GrowDirection.Both;
			chevron.GrowVertical = GrowDirection.Both;
			AddChild(chevron);
		}
	}
}
