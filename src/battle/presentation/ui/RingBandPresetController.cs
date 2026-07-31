using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class RingBandPresetController : CanvasLayer
{
	public event Action<ERingBandPreset>? PresetChanged;

	private readonly OptionButton _optionButton = new();
	private bool _syncing;

	public RingBandPresetController()
	{
		Layer = 10;
		Build();
	}

	public void Sync(ERingBandPreset preset, bool showInMoveMode)
	{
		Visible = showInMoveMode;
		if (!showInMoveMode)
			return;

		_syncing = true;
		for (var i = 0; i < RingBandPresetLabels.All.Count; i++)
		{
			if (RingBandPresetLabels.All[i] == preset)
			{
				_optionButton.Select(i);
				break;
			}
		}

		_syncing = false;
	}

	private void Build()
	{
		var margin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.TopRight,
			AnchorLeft = 1f,
			AnchorTop = 0f,
			AnchorRight = 1f,
			AnchorBottom = 0f,
			OffsetLeft = -280f,
			OffsetTop = 8f,
			OffsetRight = -8f,
			OffsetBottom = 72f,
			GrowHorizontal = Control.GrowDirection.Begin,
		};
		margin.AddThemeConstantOverride("margin_left", 8);
		margin.AddThemeConstantOverride("margin_right", 8);
		AddChild(margin);

		var panel = new PanelContainer();
		margin.AddChild(panel);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 4);
		panel.AddChild(column);

		column.AddChild(new Label { Text = "Ring bands:" });

		foreach (var preset in RingBandPresetLabels.All)
			_optionButton.AddItem(RingBandPresetLabels.Label(preset));

		_optionButton.ItemSelected += index =>
		{
			if (_syncing)
				return;

			var preset = RingBandPresetLabels.All[(int)index];
			PresetChanged?.Invoke(preset);
		};
		column.AddChild(_optionButton);
	}
}
