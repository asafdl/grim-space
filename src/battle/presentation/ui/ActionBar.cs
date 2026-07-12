using Godot;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class ActionBar : CanvasLayer
{
	public event Action<EPlayerMode>? ModeChanged;
	public event Action<EMissileMount>? MissileMountSelected;
	public event Action? EndTurnRequested;

	private readonly ButtonGroup _modeGroup = new();
	private Button _moveButton = null!;
	private Button _dorsalMissileButton = null!;
	private Button _railgunButton = null!;
	private Button _endTurnButton = null!;

	public ActionBar()
	{
		Layer = 10;
		Build();
	}

	public void SetMode(EPlayerMode mode, EMissileMount? missileMount)
	{
		_moveButton.SetBlockSignals(true);
		_dorsalMissileButton.SetBlockSignals(true);
		_railgunButton.SetBlockSignals(true);

		_moveButton.ButtonPressed = mode == EPlayerMode.Move;
		_dorsalMissileButton.ButtonPressed = mode == EPlayerMode.Missile && missileMount == EMissileMount.Dorsal;
		_railgunButton.ButtonPressed = mode == EPlayerMode.Railgun;

		_moveButton.SetBlockSignals(false);
		_dorsalMissileButton.SetBlockSignals(false);
		_railgunButton.SetBlockSignals(false);
	}

	public void Configure(int missilesRemaining, int missilesMax, bool canAct)
	{
		_dorsalMissileButton.Text = $"Dorsal Missile ({missilesRemaining}/{missilesMax})";
		_dorsalMissileButton.Disabled = !canAct || missilesRemaining <= 0;
		_moveButton.Disabled = !canAct;
		_railgunButton.Disabled = !canAct;
		_endTurnButton.Disabled = !canAct;
	}

	private void Build()
	{
		var margin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.BottomWide,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetTop = -72f,
			GrowHorizontal = Control.GrowDirection.Both,
		};
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_bottom", 16);
		AddChild(margin);

		var panel = new PanelContainer();
		margin.AddChild(panel);

		var row = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		row.AddThemeConstantOverride("separation", 8);
		panel.AddChild(row);

		_moveButton = CreateModeButton("Move", () => ModeChanged?.Invoke(EPlayerMode.Move));
		_dorsalMissileButton = CreateModeButton(
			"Dorsal Missile",
			() => MissileMountSelected?.Invoke(EMissileMount.Dorsal));
		_railgunButton = CreateModeButton("Railgun", () => ModeChanged?.Invoke(EPlayerMode.Railgun));
		_endTurnButton = CreateActionButton("End Turn");

		row.AddChild(_moveButton);
		row.AddChild(_dorsalMissileButton);
		row.AddChild(_railgunButton);
		row.AddChild(new VSeparator());
		row.AddChild(_endTurnButton);

		_moveButton.ButtonPressed = true;
	}

	private Button CreateModeButton(string text, Action onPressed)
	{
		var button = new Button
		{
			Text = text,
			ToggleMode = true,
			ButtonGroup = _modeGroup,
			CustomMinimumSize = new Vector2(140, 40),
		};
		button.Toggled += pressed =>
		{
			if (pressed)
				onPressed();
		};
		return button;
	}

	private Button CreateActionButton(string text)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(120, 40),
		};
		button.Pressed += () => EndTurnRequested?.Invoke();
		return button;
	}
}
