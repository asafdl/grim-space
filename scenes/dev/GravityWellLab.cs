using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Scenes.Dev;

public partial class GravityWellLab : Node3D
{
	private const int GridSize = 3;
	private Controller _camera = null!;
	private GravityWellsView _wellsView = null!;
	private Label _simulationStatus = null!;
	private bool _simulationEnabled = true;

	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.008f, 0.012f, 0.022f),
			},
		});

		_camera = new Controller { Name = "Camera3D", Current = true };
		AddChild(_camera);
		ResetCamera();

		var center = new Coord(1, 1, 1);
		var well = GravityWell.Create("gravitywell-lab", center, BodyFrame.WorldAligned(center),
			new BoundedGrid(GridSize, GridSize, GridSize));
		_wellsView = new GravityWellsView { Name = "GravityWells" };
		_wellsView.Configure(_camera);
		AddChild(_wellsView);
		_wellsView.SetSimulationEnabled(_simulationEnabled);
		_wellsView.Apply([new GravityWellPresentation(well.Id, well.Center, well.Frame, well.Faces)]);

		var overlay = new CanvasLayer();
		AddChild(overlay);
		overlay.AddChild(new Label
		{
			Position = new Vector2(24, 24),
			Text = $"GRAVITY WELL LAB — SURFACE FLOW (Y IS UP)\n"
				+ $"Hold {GameInputBindings.Label("battle_camera_orbit")} and drag to orbit  ·  "
				+ $"{GameInputBindings.Label("battle_zoom_in")} / {GameInputBindings.Label("battle_zoom_out")} to zoom  ·  R to reset\n"
				+ "Flow spins around the well's dorsal axis, using the standard heading colors.",
		});
		_simulationStatus = new Label
		{
			Position = new Vector2(24, 126),
			Text = "Simulation: ON (B to toggle)",
		};
		overlay.AddChild(_simulationStatus);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.R })
		{
			ResetCamera();
			GetViewport().SetInputAsHandled();
		}
		else if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.B })
		{
			_simulationEnabled = !_simulationEnabled;
			_wellsView.SetSimulationEnabled(_simulationEnabled);
			_simulationStatus.Text = $"Simulation: {(_simulationEnabled ? "ON" : "OFF")} (B to toggle)";
			GetViewport().SetInputAsHandled();
		}
	}

	private void ResetCamera()
	{
		var center = Vector3.One * (GridSize * WorldMapping.CellSize * 0.5f);
		_camera.SetFocus(center, 14f, Mathf.Pi / 4f, 0.45f);
	}
}
