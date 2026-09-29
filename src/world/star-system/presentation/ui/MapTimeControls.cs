using System;
using Godot;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class MapTimeControls : MarginContainer
{
	private const float ButtonMinSize = 40f;
	private static readonly StringName PauseIconPath = "res://assets/ui/map/time-pause.svg";
	private static readonly StringName PlayIconPath = "res://assets/ui/map/time-play.svg";
	private static readonly StringName FastForwardIconPath = "res://assets/ui/map/time-fast-forward.svg";

	private Texture2D _pauseIcon = null!;
	private Texture2D _playIcon = null!;
	private Texture2D _fastForwardIcon = null!;
	private Button _pauseButton = null!;
	private Button _speedButton = null!;

	public event Action? PausePressed;
	public event Action? SpeedPressed;

	public override void _Ready()
	{
		HudThemes.Apply(this, HudThemeFamily.Debug);
		_pauseIcon = GD.Load<Texture2D>(PauseIconPath);
		_playIcon = GD.Load<Texture2D>(PlayIconPath);
		_fastForwardIcon = GD.Load<Texture2D>(FastForwardIconPath);

		SetAnchorsPreset(LayoutPreset.CenterTop);
		GrowHorizontal = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;
		AddThemeConstantOverride("margin_top", HudStyles.Margin);

		var center = new CenterContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		AddChild(center);

		var buttons = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		buttons.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		center.AddChild(buttons);

		_pauseButton = CreateIconButton("Pause");
		_pauseButton.Pressed += () => PausePressed?.Invoke();
		buttons.AddChild(_pauseButton);

		_speedButton = CreateSpeedButton();
		_speedButton.Pressed += () => SpeedPressed?.Invoke();
		buttons.AddChild(_speedButton);
	}

	public void Sync(bool paused, float speed)
	{
		_pauseButton.Icon = paused ? _playIcon : _pauseIcon;
		_pauseButton.TooltipText = paused ? "Resume" : "Pause";
		_speedButton.Text = FormatSpeed(speed);
	}

	private Button CreateIconButton(string tooltip)
	{
		var button = new Button
		{
			ThemeTypeVariation = "DebugButton",
			CustomMinimumSize = new Vector2(ButtonMinSize, ButtonMinSize),
			ExpandIcon = true,
			Icon = _pauseIcon,
			TooltipText = tooltip,
		};
		ApplyObjectiveAccentIconColors(button);
		return button;
	}

	private Button CreateSpeedButton()
	{
		var button = new Button
		{
			ThemeTypeVariation = "DebugButton",
			CustomMinimumSize = new Vector2(ButtonMinSize + 28f, ButtonMinSize),
			ExpandIcon = true,
			Icon = _fastForwardIcon,
			Text = "1×",
			TooltipText = "Change speed",
		};
		ApplyObjectiveAccentIconColors(button);
		return button;
	}

	private static void ApplyObjectiveAccentIconColors(Button button)
	{
		var accent = HudStyles.AccentCyan;
		button.AddThemeColorOverride("icon_normal_color", accent);
		button.AddThemeColorOverride("icon_hover_color", accent);
		button.AddThemeColorOverride("icon_pressed_color", accent);
		button.AddThemeColorOverride("icon_focus_color", accent);
		button.AddThemeColorOverride("icon_disabled_color", accent with { A = 0.45f });
	}

	private static string FormatSpeed(float speed) =>
		$"{speed:0}×";
}
