using Godot;
using GrimSpace.Battle.Movement.Enums;
using ShipOrientation = GrimSpace.Battle.Movement.Orientation;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class ShipOrientationHud : CanvasLayer
{
	public event Action<EHeadingTurn>? HeadingTurnRequested;
	public event Action<ERollDirection>? RollRequested;

	public ShipOrientationHud()
	{
		Layer = 10;
		Build();
		Visible = false;
	}

	public void Show(bool show) => Visible = show;

	private void Build()
	{
		var margin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.BottomLeft,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			GrowVertical = Control.GrowDirection.Begin,
		};
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_bottom", 88);
		AddChild(margin);

		var panel = new PanelContainer();
		margin.AddChild(panel);

		var content = new MarginContainer();
		content.AddThemeConstantOverride("margin_left", 6);
		content.AddThemeConstantOverride("margin_right", 6);
		content.AddThemeConstantOverride("margin_top", 6);
		content.AddThemeConstantOverride("margin_bottom", 6);
		panel.AddChild(content);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 4);
		content.AddChild(column);

		column.AddChild(new Label { Text = "Orientation" });

		var grid = new GridContainer { Columns = 3 };
		grid.AddThemeConstantOverride("h_separation", 4);
		grid.AddThemeConstantOverride("v_separation", 4);
		column.AddChild(grid);

		grid.AddChild(Spacer());
		grid.AddChild(HeadingButton("▲", EHeadingTurn.PitchUp));
		grid.AddChild(Spacer());

		grid.AddChild(HeadingButton("◀", EHeadingTurn.YawLeft));
		grid.AddChild(RollColumn());
		grid.AddChild(HeadingButton("▶", EHeadingTurn.YawRight));

		grid.AddChild(Spacer());
		grid.AddChild(HeadingButton("▼", EHeadingTurn.PitchDown));
		grid.AddChild(Spacer());
	}

	private Control RollColumn()
	{
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 2);
		column.AddChild(HeadingButton("180", EHeadingTurn.Yaw180));
		column.AddChild(RollButton("↺", ERollDirection.CounterClockwise));
		column.AddChild(RollButton("↻", ERollDirection.Clockwise));
		return column;
	}

	private static Control Spacer() =>
		new() { CustomMinimumSize = new Vector2(36, 32) };

	private Button HeadingButton(string symbol, EHeadingTurn turn)
	{
		var button = new Button
		{
			Text = symbol,
			CustomMinimumSize = new Vector2(36, 32),
			TooltipText = ShipOrientation.IsYawTurn(turn)
				? $"Yaw {turn} (net 0°=0 AP, 90°=1 AP, 180°=2 AP on commit)"
				: $"Pitch {turn} (1 AP)",
		};
		button.Pressed += () => HeadingTurnRequested?.Invoke(turn);
		return button;
	}

	private Button RollButton(string symbol, ERollDirection direction)
	{
		var button = new Button
		{
			Text = symbol,
			CustomMinimumSize = new Vector2(36, 30),
			TooltipText = $"Roll {direction} (1 AP)",
		};
		button.Pressed += () => RollRequested?.Invoke(direction);
		return button;
	}
}
