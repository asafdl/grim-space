using Godot;
using GrimSpace.Application;
using GrimSpace.Components;
using GrimSpace.Education;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;
using GrimSpace.World.StarSystem.Vision;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapController : Node3D
{
	private const float SecondsPerTick = 0.2f;
	private const int TutorialDialogWidth = 380;
	private const int TutorialDialogTop = 270;
	private static readonly float[] SpeedOptions = [0.5f, 1f, 2f, 4f, 8f];

	private MapView _view = null!;
	private RoutesView _routes = null!;
	private UnitsView _units = null!;
	private CourseView _course = null!;
	private MapCamera _camera = null!;
	private Label _tooltip = null!;
	private Label _tickLabel = null!;
	private Label _systemLabel = null!;
	private Button _pauseButton = null!;
	private Button _stepButton = null!;
	private Button _speedButton = null!;
	private Button _rebuildButton = null!;
	private Button _overviewButton = null!;
	private Button _accessButton = null!;
	private CanvasLayer _uiLayer = null!;
	private ObjectivesHud _objectivesHud = null!;
	private ResourceHud _resourceHud = null!;
	private EngagementController _engagement = null!;
	private NarrativeController _narrative = null!;
	private TutorialDialog? _tutorialDialog;
	private TutorialController? _tutorial;
	private IWorldFocus _worldFocus = null!;
	private IWorldIndicator _worldIndicator = null!;
	private IDisposable _engageSubscription = null!;
	private ResourceTransactionFeed _resourceTransactions = null!;

	private StarSystemOrchestrator _orchestrator = null!;
	private UserIntentTranslator _intentTranslator = null!;
	private WorldMapDirector _director = null!;
	private FacadePresentationMode _facadeMode = null!;
	private float _tickAccumulator;
	private int _speedIndex = 1;
	private float _unreachableFlashTimer;
	private bool _battleTransitionPending;
	private bool _staleWaitingForPlayerInputReported;
	private IReadOnlySet<string> _playerVisibleFleetIds = new HashSet<string>(StringComparer.Ordinal);

	public override void _Ready()
	{
		_view = GetNode<MapView>("MapView");
		_routes = GetNode<RoutesView>("RoutesView");
		_units = GetNode<UnitsView>("UnitsView");
		_course = GetNode<CourseView>("CourseView");
		_camera = GetNode<MapCamera>("CameraPivot/SpringArm3D/Camera3D");

		_uiLayer = GetNode<CanvasLayer>("UI");
		_tooltip = GetNode<Label>("UI/Tooltip");
		HudThemes.Apply(_tooltip, HudThemeFamily.Debug);
		var debugHud = GetNode<DebugHud>("UI/DebugHud");
		_systemLabel = debugHud.SystemLabel;
		_tickLabel = debugHud.TickLabel;
		_pauseButton = debugHud.PauseButton;
		_stepButton = debugHud.StepButton;
		_speedButton = debugHud.SpeedButton;
		_rebuildButton = debugHud.RebuildButton;
		_overviewButton = debugHud.OverviewButton;
		_accessButton = GetNode<Button>("UI/AccessButton");
		_objectivesHud = GetNode<ObjectivesHud>("UI/ObjectivesHud");
		_resourceHud = GetNode<ResourceHud>("UI/ResourceHud");

		_orchestrator = Session.Instance.Run.StarSystem;
		_orchestrator.RefreshPlayerAgent();
		_resourceTransactions = new ResourceTransactionFeed();
		_resourceTransactions.Bind(
			Session.Instance.TransitionInbox,
			_orchestrator,
			_resourceHud);

		var engagementHud = new EngagementHudOverlay();
		_uiLayer.AddChild(engagementHud);
		_engagement = new EngagementController(
			engagementHud,
			() => EngagementQueries.TryGetPendingPlayerEngagement(
				_orchestrator.Map,
				State.PlayerFleetUnitId,
				out var pending)
					? pending
					: null,
			() => _orchestrator.PlayerAgent!.TryEnqueue(
				[new EngageAction(State.PlayerFleetUnitId)]),
			() => _orchestrator.PlayerAgent!.TryEnqueue(
				[new FleeAction(State.PlayerFleetUnitId)]),
			sync => _orchestrator.Subscribe<ReachContactAction>(_ => sync()));
		_engageSubscription = _orchestrator.Subscribe<EngageAction>(OnEngagementCommitted);
		if (EngagementQueries.TryGetCommittedPlayerEngagement(
			_orchestrator.Map,
			State.PlayerFleetUnitId,
			out _))
		{
			DeferBattleTransition();
		}
		_intentTranslator = new UserIntentTranslator(
			_orchestrator.PlayerAgent!,
			_camera,
			() => GetViewport().GetMousePosition(),
			() => _orchestrator.Map.Width,
			() => _orchestrator.Map.Height,
			picked => _view.ResolveMoveDestination(picked),
			point =>
			{
				var tickFraction = _tickAccumulator / SecondsPerTick;
				return _units.UnitAt(_orchestrator, point, tickFraction, IsPlayerFleetVisible)?.UnitId;
			});
		_pauseButton.Pressed += () => _orchestrator.TogglePause();
		_stepButton.Pressed += () =>
		{
			_orchestrator.Step();
			_tickAccumulator = 0f;
		};
		_speedButton.Pressed += () => CycleSpeed(1);
		_rebuildButton.Pressed += RebuildScene;
		_overviewButton.Pressed += OnOverviewButtonPressed;
		_accessButton.Pressed += OnAccessButtonPressed;

		var world = _orchestrator.Map;
		var halfX = world.Width * MapMapping.WorldUnitsPerPoint * 0.5f;
		var halfZ = world.Height * MapMapping.WorldUnitsPerPoint * 0.5f;
		var atmosphere = MapAtmosphereSettings.Default;

		var backdrop = new MapBackdrop();
		backdrop.Build(atmosphere);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_view.ConfigureAtmosphere(atmosphere);
		_view.Build(world);

		var star = world.PointsOfInterest.OfType<Star>().First();
		MapStarLighting.Configure(
			GetNode<DirectionalLight3D>("DirectionalLight3D"),
			star,
			world.Width,
			world.Height,
			atmosphere);
		_routes.Build(world);
		_units.Build(world);
		_course.Build(world);
		_camera.Configure(Vector3.Zero, halfX, halfZ);

		var fadeOverlay = GetNode<ColorRect>("UI/FadeOverlay");
		var presentationContext = new MapPresentationContext
		{
			Map = () => _orchestrator.Map,
			ResolvePlayerTravelSample = ResolvePlayerTravelSample,
			ResolveDockedPoiId = () => ResolveDockedPoiId(_orchestrator.Map),
			CanAccessFacilities = () => !IsBlockingModalOpen(),
			ViewportSize = () =>
			{
				var size = GetViewport().GetVisibleRect().Size;
				return (size.X, size.Y);
			},
			Camera = _camera,
			ResolveCameraPose = () => _camera.CurrentPose,
			IsCameraAnimating = () => _camera.IsAnimating,
			ApplyCameraDistanceDelta = delta => _camera.ApplyDistanceDelta(delta),
			View = _view,
			BoundsHalfX = halfX,
			BoundsHalfZ = halfZ,
			ApplyLimits = limits => _camera.ApplyLimits(limits),
			SetOcclusionEnabled = enabled => _camera.SetOcclusionEnabled(enabled),
			SnapToPose = (pose, limits) => _camera.SnapToPose(pose, limits),
			TweenToPose = (pose, limits, onComplete) => _camera.TweenToPose(pose, limits, onComplete),
		};
		_director = new WorldMapDirector(presentationContext);
		_facadeMode = new FacadePresentationMode(
			_uiLayer,
			fadeOverlay,
			() => _director.TryExit(FacadePresentationMode.ModeId));
		_facadeMode.FacilityEntered += OnFacilityEntered;
		_director.RegisterMode(new CinematicPresentationMode(_accessButton));
		_director.RegisterMode(new OverviewPresentationMode());
		_director.RegisterMode(_facadeMode);

		_worldFocus = new MapWorldFocus(
			_camera,
			() => _orchestrator.Map,
			CommittedPositionOf,
			onReady => _director.PrepareForFocus(onReady).Succeeded,
			IsPlayerFleetVisible);
		var worldIndicators = new MapWorldIndicators();
		worldIndicators.Configure(
			() => _orchestrator.Map,
			CommittedPositionOf,
			() => _director.CurrentModeId is CinematicPresentationMode.ModeId
				or OverviewPresentationMode.ModeId,
			() => new WorldArrowIndicator(),
			objectId => _view.GetIndicatorClearance(objectId, _orchestrator.Map),
			IsPlayerFleetVisible);
		AddChild(worldIndicators);
		_worldIndicator = worldIndicators;

		var narrativeHud = new NarrativeHudOverlay();
		_uiLayer.AddChild(narrativeHud);
		_narrative = new NarrativeController(
			narrativeHud,
			_worldFocus,
			_worldIndicator,
			ResolveNarrative(_orchestrator.Map.ActiveNarrativeId),
			narrativeId => ResolveNarrative(narrativeId),
			narrativeId => _orchestrator.PlayerAgent!.TryEnqueue(
				[new CompleteNarrativeAction(State.PlayerFleetUnitId, narrativeId)]),
			onBegin => _orchestrator.Subscribe<BeginNarrativeAction>(
				action => onBegin(action.NarrativeId)));
		if (GameSettings.ReadShowTutorials())
		{
			_tutorialDialog = new TutorialDialog();
			_uiLayer.AddChild(_tutorialDialog);
			ConfigureTutorialDialog(_tutorialDialog);
			_tutorial = new TutorialController(
				_orchestrator,
				Session.Instance.Run.TutorialProgress,
				_tutorialDialog,
				new WorldLinkNavigator(_worldFocus, _worldIndicator));
		}

		_orchestrator.PlayerAgent!.PlanningChanged += OnPlayerPlanningChanged;

		UpdateSystemLabel(world);
		UpdateDebugUi();
		UpdateObjectivesHud();

		if (MapNavigationContext.ReturnToFacade && MapNavigationContext.ActivePoiId is { } returnPoiId)
		{
			_director.SetInitialMode(
				FacadePresentationMode.ModeId,
				new FacadeEnterPayload(returnPoiId));
			MapNavigationContext.ClearReturnToFacade();
		}
		else
		{
			_director.SetInitialMode(CinematicPresentationMode.ModeId);
		}

		_tutorial?.Sync();

		RefreshPlayerVisibleFleets(0f);
		_units.Sync(_orchestrator, 0f, IsPlayerFleetVisible);
	}

	public override void _Process(double delta)
	{
		_view.SetCameraDistance(_camera.Distance);

		if (_orchestrator.CanAdvance && !_director.IsTransitioning)
			AdvanceSimulation(delta);

		var world = _orchestrator.Map;
		ReportStaleWaitingForPlayerInputInvariant(world);
		_camera.ApplyInputPolicy(_director.EffectiveInputPolicy, IsBlockingModalOpen());
		var tickFraction = _tickAccumulator / SecondsPerTick;
		RefreshPlayerVisibleFleets(tickFraction);
		_units.Sync(_orchestrator, tickFraction, IsPlayerFleetVisible);
		if (_unreachableFlashTimer > 0f)
			_unreachableFlashTimer = Mathf.Max(0f, _unreachableFlashTimer - (float)delta);
		_course.Sync(_orchestrator, _unreachableFlashTimer > 0f, tickFraction);
		UpdateDebugUi();
		UpdateObjectivesHud();
		_director.Update(delta);

		if (!_director.EffectiveInputPolicy.AllowsStrategicHover)
		{
			_tooltip.Visible = false;
			return;
		}

		var screen = GetViewport().GetMousePosition();
		var point = MapPick.PickPoint(_camera, screen, world.Width, world.Height);
		var unitHover = point is { } unitPoint
			? _units.UnitAt(_orchestrator, unitPoint, tickFraction, IsPlayerFleetVisible)
			: null;
		var dockHover = unitHover is null && point is { } dockPoint ? _view.DockAt(dockPoint) : null;
		var poiId = dockHover is null && unitHover is null && point is { } pick ? _view.PoiAt(pick) : null;
		_view.SetHovered(poiId);
		UpdateTooltip(world, poiId, dockHover, unitHover, screen);
	}

	public override void _ExitTree()
	{
		_resourceTransactions?.Dispose();
		if (_orchestrator.PlayerAgent is not null)
			_orchestrator.PlayerAgent.PlanningChanged -= OnPlayerPlanningChanged;
		_engageSubscription.Dispose();
		_engagement.Dispose();
		_narrative.Dispose();
		_tutorial?.Dispose();
		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_narrative.TryHandleInput(@event)
			|| _engagement.TryHandleInput(@event))
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (IsBlockingModalOpen())
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_director.FilterInput(@event))
		{
			if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
				GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseButton { Pressed: true } wheel
			&& wheel.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
		{
			if (GetViewport().GuiGetHoveredControl() is not null)
				return;

			if (!_director.EffectiveInputPolicy.AllowsWheelZoom)
				return;

			var direction = wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
			_director.OnWheelZoom(direction);
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseButton mouseButton)
		{
			if (GetViewport().GuiGetHoveredControl() is not null)
				return;

			if (mouseButton.ButtonIndex == MouseButton.Left)
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
				GetViewport().SetInputAsHandled();
				GetTree().ChangeSceneToFile("res://scenes/main.tscn");
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

	private bool IsBlockingModalOpen() => _narrative.IsOpen || _engagement.IsOpen;

	private void ReportStaleWaitingForPlayerInputInvariant(StarMap world)
	{
		var stale = world.WaitingForPlayerInput && !IsBlockingModalOpen();
		if (stale && !_staleWaitingForPlayerInputReported)
		{
			GD.PushWarning(
				"StarMap.WaitingForPlayerInput is true but no narrative or engagement modal is open.");
			_staleWaitingForPlayerInputReported = true;
		}
		else if (!stale)
			_staleWaitingForPlayerInputReported = false;
	}

	private void AdvanceSimulation(double delta)
	{
		_tickAccumulator += (float)delta * SpeedOptions[_speedIndex];
		while (_tickAccumulator >= SecondsPerTick && _orchestrator.CanAdvance)
		{
			_tickAccumulator -= SecondsPerTick;
			_orchestrator.AdvanceTick();
			if (!_orchestrator.CanAdvance)
			{
				_tickAccumulator = 0f;
				break;
			}
		}
	}

	private Coord CommittedPositionOf(string unitId) =>
		_orchestrator.CommittedPositionOf(unitId, _tickAccumulator / SecondsPerTick);

	private bool IsPlayerFleetVisible(string fleetId) =>
		_playerVisibleFleetIds.Contains(fleetId);

	private void RefreshPlayerVisibleFleets(float tickFraction)
	{
		_playerVisibleFleetIds = FleetVisionQueries.VisibleTo(
			_orchestrator.Map,
			_orchestrator.PlayerId ?? "",
			_orchestrator.RuntimeFor,
			tickFraction);
	}

	private NarrativeDefinition? ResolveNarrative(string? narrativeId) =>
		narrativeId is not null
		&& MapNarratives.TryGet(narrativeId, _orchestrator.Map, out var narrative)
			? narrative
			: null;

	private void OnEngagementCommitted(EngageAction engage)
	{
		if (engage.ActorId == State.PlayerFleetUnitId)
			DeferBattleTransition();
	}

	private void OnPlayerPlanningChanged()
	{
		if (_orchestrator.PlayerAgent?.PendingCourse is null)
			return;

		_director.OnPlayerMovement();
	}

	private PlayerTravelSample ResolvePlayerTravelSample()
	{
		var world = _orchestrator.Map;
		var unit = world.FleetRegistry.FleetOf(State.PlayerFleetUnitId);
		var tickFraction = _tickAccumulator / SecondsPerTick;
		var cachedPath = _orchestrator.RuntimeFor(State.PlayerFleetUnitId).CachedPath;
		var continuousPosition = unit.State.CommittedPositionContinuous(
			world,
			cachedPath,
			tickFraction);
		if (continuousPosition is null)
		{
			var (position, tangent) = unit.State.CommittedPosition(world, cachedPath, tickFraction);
			return MapPlayerTravelSample.Resolve(
				world.Width,
				world.Height,
				position,
				tangent,
				_orchestrator.PlayerAgent?.PendingCourse,
				unit.State.SpeedPerTick);
		}

		return MapPlayerTravelSample.Resolve(
			world.Width,
			world.Height,
			continuousPosition,
			_orchestrator.PlayerAgent?.PendingCourse,
			unit.State.SpeedPerTick);
	}

	private void DeferBattleTransition()
	{
		if (_battleTransitionPending)
			return;

		_battleTransitionPending = true;
		Callable.From(BeginBattleTransition).CallDeferred();
	}

	private void BeginBattleTransition()
	{
		_battleTransitionPending = false;
		if (!Session.Instance.BeginEngagement(State.PlayerFleetUnitId))
			return;

		GetTree().ChangeSceneToFile("res://scenes/battle.tscn");
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
		_pauseButton.Disabled = false;
		_stepButton.Disabled = !_orchestrator.IsStepped;
		_speedButton.Text = $"Speed {SpeedOptions[_speedIndex]:0.#}x";
		_overviewButton.Text = _director.CurrentModeId == OverviewPresentationMode.ModeId
			? "Exit Overview"
			: "Overview";
	}

	private void OnOverviewButtonPressed()
	{
		if (_director.CurrentModeId == OverviewPresentationMode.ModeId)
			_director.TryExit(OverviewPresentationMode.ModeId);
		else
			_director.TryEnter(OverviewPresentationMode.ModeId);
	}

	private void OnAccessButtonPressed()
	{
		var dockedPoiId = ResolveDockedPoiId(_orchestrator.Map);
		if (dockedPoiId is null)
			return;

		_director.TryEnter(FacadePresentationMode.ModeId, new FacadeEnterPayload(dockedPoiId));
	}

	private static string? ResolveDockedPoiId(StarMap world)
	{
		var player = world.FleetRegistry.FleetOf(State.PlayerFleetUnitId);
		if (player.State.Phase != Units.EPhase.Docked
			|| string.IsNullOrEmpty(player.State.DockedAtDockId))
			return null;

		return world.DocksById[player.State.DockedAtDockId].PoiId;
	}

	private void CycleSpeed(int delta)
	{
		_speedIndex = Mathf.PosMod(_speedIndex + delta, SpeedOptions.Length);
	}

	private void RebuildScene()
	{
		Session.Instance.RegenerateMap();
		GetTree().ReloadCurrentScene();
	}

	private void UpdateObjectivesHud()
	{
		var objectives = ObjectivesCollector.Collect(_orchestrator.Map, State.PlayerFleetUnitId);
		_objectivesHud.Sync(objectives);
	}

	private static void ConfigureTutorialDialog(TutorialDialog dialog)
	{
		dialog.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		dialog.OffsetLeft = -TutorialDialogWidth - HudStyles.Margin;
		dialog.OffsetTop = TutorialDialogTop;
		dialog.OffsetRight = -HudStyles.Margin;
		dialog.OffsetBottom = TutorialDialogTop;
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
			_tooltip.Text = $"{unitHover.Type} ({unitHover.UnitId})";
			_tooltip.Visible = true;
			_tooltip.Position = screen + new Vector2(14, 18);
			return;
		}

		if (dockHover is not null)
		{
			_tooltip.Text = dockHover.DisplayName;
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
		_tooltip.Text = poi.DisplayName;
		_tooltip.Visible = true;
		_tooltip.Position = screen + new Vector2(14, 18);
	}
}
