using Godot;
using GrimSpace.Battle.Presentation.Graphics;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class GridDotStridePanel : CanvasLayer
{
	public event Action<int>? StrideChanged;
	public event Action<bool>? CameraPlaneToggled;

	private SpinBox _spinBox = null!;
	private Label _planeLabel = null!;
	private CheckBox _cameraPlaneCheck = null!;

	public GridDotStridePanel(int initialStride)
	{
		Layer = 10;
		Build(initialStride);
	}

	public void SetActivePlaneLabel(string? planeLabel)
	{
		_planeLabel.Text = planeLabel is null ? "" : $"Plane {planeLabel}";
	}

	private void Build(int initialStride)
	{
		var margin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.TopLeft,
			OffsetRight = 280f,
			OffsetBottom = 88f,
			GrowHorizontal = Control.GrowDirection.End,
			GrowVertical = Control.GrowDirection.End,
		};
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_top", 16);
		AddChild(margin);

		var panel = new PanelContainer();
		margin.AddChild(panel);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 6);
		panel.AddChild(column);

		var strideRow = new HBoxContainer();
		strideRow.AddThemeConstantOverride("separation", 8);
		column.AddChild(strideRow);

		strideRow.AddChild(new Label { Text = "Dot stride" });

		_spinBox = new SpinBox
		{
			MinValue = 1,
			MaxValue = 32,
			Value = initialStride,
			CustomMinimumSize = new Vector2(72, 0),
			Rounded = true,
			Alignment = HorizontalAlignment.Center,
		};
		_spinBox.ValueChanged += value => StrideChanged?.Invoke((int)value);
		strideRow.AddChild(_spinBox);

		var planeRow = new HBoxContainer();
		planeRow.AddThemeConstantOverride("separation", 8);
		column.AddChild(planeRow);

		_cameraPlaneCheck = new CheckBox { Text = "Camera plane" };
		_cameraPlaneCheck.Toggled += on => CameraPlaneToggled?.Invoke(on);
		planeRow.AddChild(_cameraPlaneCheck);

		_planeLabel = new Label { Text = "" };
		planeRow.AddChild(_planeLabel);
	}
}
