using Godot;
using GrimSpace.Core;
using GrimSpace.World.StarSystem;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapController : Node3D
{
	private const float SecondsPerTick = 0.2f;
	private static readonly float[] SpeedOptions = [0.5f, 1f, 2f, 4f, 8f];

	private MapView _view = null!;
	private RoutesView _routes = null!;
	private UnitsView _units = null!;
	private CourseView _course = null!;
	private MapCamera _camera = null!;
	private PanelContainer _tooltip = null!;
	private Label _typeLabel = null!;
	private Label _nameLabel = null!;
	private Label _tickLabel = null!;
	private Label _systemLabel = null!;
	private Button _pauseButton = null!;
	private Button _stepButton = null!;
	private Button _speedButton = null!;
	private Button _rebuildButton = null!;
	private CanvasLayer _uiLayer = null!;

	private StarSystemOrchestrator _orchestrator = null!;
	private UserIntentTranslator _intentTranslator = null!;
	private MapPoiFacade _poiFacade = null!;
	private float _tickAccumulator;
	private int _speedIndex = 1;
	private float _unreachableFlashTimer;

	public override void _Ready()
	{
		_view = GetNode<MapView>("MapView");
		_routes = GetNode<RoutesView>("RoutesView");
		_units = GetNode<UnitsView>("UnitsView");
		_course = GetNode<CourseView>("CourseView");
		_camera = GetNode<MapCamera>("Camera3D");

		_uiLayer = GetNode<CanvasLayer>("UI");
		_tooltip = GetNode<PanelContainer>("UI/Tooltip");
		_typeLabel = GetNode<Label>("UI/Tooltip/VBoxContainer/TypeLabel");
		_nameLabel = GetNode<Label>("UI/Tooltip/VBoxContainer/NameLabel");
		_tickLabel = GetNode<Label>("UI/DebugPanel/MarginContainer/HBoxContainer/TickLabel");
		_pauseButton = GetNode<Button>("UI/DebugPanel/MarginContainer/HBoxContainer/PauseButton");
		_stepButton = GetNode<Button>("UI/DebugPanel/MarginContainer/HBoxContainer/StepButton");
		_speedButton = GetNode<Button>("UI/DebugPanel/MarginContainer/HBoxContainer/SpeedButton");
		_rebuildButton = GetNode<Button>("UI/DebugPanel/MarginContainer/HBoxContainer/RebuildButton");
		_systemLabel = GetNode<Label>("UI/SystemLabel");

		_orchestrator = RunSession.Instance.Run.StarSystem;
		_intentTranslator = new UserIntentTranslator(
			_orchestrator.PlayerAgent!,
			_camera,
			() => GetViewport().GetMousePosition(),
			() => _orchestrator.Map.Width,
			() => _orchestrator.Map.Height,
			picked => _view.ResolveMoveDestination(picked));
		_pauseButton.Pressed += () => _orchestrator.TogglePause();
		_stepButton.Pressed += () =>
		{
			_orchestrator.Step();
			_tickAccumulator = 0f;
		};
		_speedButton.Pressed += () => CycleSpeed(1);
		_rebuildButton.Pressed += RebuildScene;

		_poiFacade = new MapPoiFacade(
			_view,
			_camera,
			() => _orchestrator.Map,
			() => GetViewport().GetVisibleRect().Size,
			_uiLayer,
			GetNode<Button>("UI/AccessButton"),
			GetNode<ColorRect>("UI/FadeOverlay"));
		_poiFacade.FacilityEntered += OnFacilityEntered;

		var world = _orchestrator.Map;
		var halfX = world.Width * MapMapping.WorldUnitsPerPoint * 0.5f;
		var halfZ = world.Height * MapMapping.WorldUnitsPerPoint * 0.5f;
		var mapRadius = Mathf.Max(halfX, halfZ);

		var backdrop = new MapBackdrop();
		backdrop.Build(mapRadius);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_view.Build(world);
		_routes.Build(world);
		_units.Build(world);
		_course.Build(world);
		_camera.Configure(Vector3.Zero, halfX, halfZ);
		UpdateSystemLabel(world);
		UpdateDebugUi();

		if (MapNavigationContext.ReturnToFacade && MapNavigationContext.ActivePoiId is { } returnPoiId)
		{
			var poi = world.PointsOfInterest.First(p => p.Id == returnPoiId);
			_poiFacade.ReEnterFacade(poi, world);
			MapNavigationContext.ClearReturnToFacade();
		}
	}

	public override void _Process(double delta)
	{
		_view.SetCameraDistance(_camera.Distance);

		if (_orchestrator.IsRunning)
			AdvanceSimulation(delta);

		var world = _orchestrator.Map;
		var tickFraction = _tickAccumulator / SecondsPerTick;
		_units.Sync(_orchestrator, tickFraction);
		if (_unreachableFlashTimer > 0f)
			_unreachableFlashTimer = Mathf.Max(0f, _unreachableFlashTimer - (float)delta);
		_course.Sync(_orchestrator, _unreachableFlashTimer > 0f);
		UpdateDebugUi();
		_poiFacade.Update();

		if (!_poiFacade.IsStrategic)
		{
			_tooltip.Visible = false;
			return;
		}

		var screen = GetViewport().GetMousePosition();
		var point = MapPick.PickPoint(_camera, screen, world.Width, world.Height);
		var unitHover = point is { } unitPoint ? _units.UnitAt(_orchestrator, unitPoint, tickFraction) : null;
		var dockHover = unitHover is null && point is { } dockPoint ? _view.DockAt(dockPoint) : null;
		var poiId = dockHover is null && unitHover is null && point is { } pick ? _view.PoiAt(pick) : null;
		_view.SetHovered(poiId);
		UpdateTooltip(world, poiId, dockHover, unitHover, screen);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_poiFacade.FilterInput(@event))
		{
			if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
				GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseButton mouseButton)
		{
			if (GetViewport().GuiGetHoveredControl() is not null)
				return;

			if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				if (_intentTranslator.TryHandleMouseButton(mouseButton, out var unreachable))
				{
					if (unreachable)
						_unreachableFlashTimer = 0.6f;

					GetViewport().SetInputAsHandled();
				}

				return;
			}
		}

		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Escape:
				GetTree().ChangeSceneToFile("res://scenes/main.tscn");
				GetViewport().SetInputAsHandled();
				break;
			case Key.Space:
				_orchestrator.TogglePause();
				GetViewport().SetInputAsHandled();
				break;
			case Key.Period when _orchestrator.IsStepped:
				_orchestrator.Step();
				_tickAccumulator = 0f;
				GetViewport().SetInputAsHandled();
				break;
			case Key.Bracketright:
				CycleSpeed(1);
				GetViewport().SetInputAsHandled();
				break;
			case Key.Bracketleft:
				CycleSpeed(-1);
				GetViewport().SetInputAsHandled();
				break;
		}
	}

	private void AdvanceSimulation(double delta)
	{
		_tickAccumulator += (float)delta * SpeedOptions[_speedIndex];
		while (_tickAccumulator >= SecondsPerTick)
		{
			_tickAccumulator -= SecondsPerTick;
			_orchestrator.AdvanceTick();
		}
	}

	private void OnFacilityEntered(FacilityEntry entry)
	{
		var scenePath = FacilityScenes.ResolveScene(entry.Facility);
		if (scenePath is null)
			return;

		MapNavigationContext.EnterFacility(entry.PoiId, entry.Facility.Id);
		GetTree().ChangeSceneToFile(scenePath);
	}

	private void UpdateDebugUi()
	{
		_tickLabel.Text = $"Tick {_orchestrator.Tick}";
		_pauseButton.Text = _orchestrator.IsStepped ? "Resume" : "Pause";
		_stepButton.Disabled = !_orchestrator.IsStepped;
		_speedButton.Text = $"Speed {SpeedOptions[_speedIndex]:0.#}x";
	}

	private void CycleSpeed(int delta)
	{
		_speedIndex = Mathf.PosMod(_speedIndex + delta, SpeedOptions.Length);
	}

	private void RebuildScene()
	{
		RunSession.Instance.RegenerateMap();
		GetTree().ReloadCurrentScene();
	}

	private void UpdateSystemLabel(StarMap world)
	{
		var blueprint = world.Blueprint;
		_systemLabel.Text = $"{blueprint.SystemClass} · seed {blueprint.Seed} · {blueprint.SupplyPlan.ResourceId}";
	}

	private void UpdateTooltip(
		StarMap world,
		string? poiId,
		MapView.DockHoverInfo? dockHover,
		UnitsView.UnitHoverInfo? unitHover,
		Vector2 screen)
	{
		if (unitHover is not null)
		{
			_typeLabel.Text = unitHover.Phase.ToString().ToUpperInvariant();
			_nameLabel.Text = $"{unitHover.Type} ({unitHover.UnitId})";
			_tooltip.Visible = true;
			_tooltip.Position = screen + new Vector2(14, 18);
			return;
		}

		if (dockHover is not null)
		{
			_typeLabel.Text = "DOCK";
			_nameLabel.Text = dockHover.DisplayName;
			_tooltip.Visible = true;
			_tooltip.Position = screen + new Vector2(14, 18);
			return;
		}

		if (poiId is null)
		{
			_tooltip.Visible = false;
			return;
		}

		var poi = world.PointsOfInterest.First(p => p.Id == poiId);
		var blueprint = world.Blueprint;
		_typeLabel.Text =
			$"{blueprint.SystemClass} · seed {blueprint.Seed} · {poi.LogicalRole} · {blueprint.SupplyPlan.ResourceId}";
		_nameLabel.Text = poi.DisplayName;
		_tooltip.Visible = true;
		_tooltip.Position = screen + new Vector2(14, 18);
	}
}
