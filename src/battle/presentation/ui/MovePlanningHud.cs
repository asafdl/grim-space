using Godot;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class MovePlanningHud : CanvasLayer
{
	private readonly Label _apValue = new();
	private readonly Label _momentumValue = new();
	private readonly Label _thrustValue = new();
	private readonly Label _ringValue = new();

	public MovePlanningHud()
	{
		Layer = 10;
		Build();
	}

	public void Sync(PresentationFrame frame)
	{
		var show = !frame.ShowOutcomeOverlay
			&& frame.Mode == EPlayerMode.Move
			&& frame.CanAct;
		Visible = show;
		if (!show)
			return;

		var ap = frame.ActorState.ActionPoints;
		_apValue.Text = ap.ToString();
		_apValue.AddThemeColorOverride(
			"font_color",
			ap >= 4 ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.85f, 0.25f));

		var momentum = frame.ActorState.MomentumLevel;
		_momentumValue.Text = $"M{momentum}";
		_momentumValue.AddThemeColorOverride(
			"font_color",
			momentum switch
			{
				0 => new Color(0.75f, 0.85f, 1f),
				1 => new Color(0.45f, 0.95f, 1f),
				_ => new Color(0.2f, 1f, 0.95f),
			});

		var table = frame.MovePreviewRingTable;
		if (table.RingCount > 0
			&& frame.ActiveRingIndex >= 0
			&& frame.ActiveRingIndex < table.RingCount)
		{
			var ring = table.RingFacetsAt(frame.ActiveRingIndex);
			var parts = new List<string>();
			if (table.Preset is ERingBandPreset.ApThrust or ERingBandPreset.ApMomentumThrust)
				parts.Add(MoveRingFacets.ThrustDisplayLabel(ring.ThrustClass));
			if (table.Preset is ERingBandPreset.ApMomentum or ERingBandPreset.ApMomentumThrust)
				parts.Add(MoveRingFacets.MomentumOutcomeDisplayLabel(ring.MomentumOutcome));
			if (table.Preset is ERingBandPreset.ApThrust or ERingBandPreset.ApMomentum or ERingBandPreset.ApMomentumThrust)
				parts.Add(MoveRingFacets.ApCostDisplayLabel(ring.ApCost));

			_thrustValue.Text = parts.Count > 0 ? string.Join(" · ", parts) : "—";
			_ringValue.Text = $"Ring {frame.ActiveRingIndex + 1} / {table.RingCount}";
		}
		else
		{
			_thrustValue.Text = "—";
			_ringValue.Text = "No rings";
		}

		_thrustValue.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.2f));
		_ringValue.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
	}

	private void Build()
	{
		var margin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.TopLeft,
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 0f,
			AnchorBottom = 0f,
			OffsetLeft = 16f,
			OffsetTop = 56f,
			OffsetRight = 420f,
			OffsetBottom = 160f,
		};
		AddChild(margin);

		var panel = new PanelContainer();
		margin.AddChild(panel);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		panel.AddChild(row);

		row.AddChild(BuildStatBlock("AP", _apValue, 28));
		row.AddChild(BuildStatBlock("Momentum", _momentumValue, 24));
		row.AddChild(BuildStatBlock("Active band", _thrustValue, 18));
		row.AddChild(BuildStatBlock("", _ringValue, 16));
	}

	private static Control BuildStatBlock(string title, Label value, int fontSize)
	{
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 0);
		if (title.Length > 0)
		{
			column.AddChild(new Label
			{
				Text = title,
				Modulate = new Color(0.75f, 0.78f, 0.85f),
			});
		}

		value.AddThemeFontSizeOverride("font_size", fontSize);
		column.AddChild(value);
		return column;
	}
}
