using Godot;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.Math.Camera;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Facilities;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Director;

public sealed class FacadePresentationMode : IPresentationMode
{
	public const string ModeId = "facade";

	private const float FacilityZoomDistance = 2.8f;
	private const float FacilityFadeDuration = 0.35f;
	private const string ManagementIconPath = "res://assets/ui/map/icons/management-icon.svg";
	private const string DockyardIconPath = "res://assets/ui/map/icons/dockyard-icon.png";
	private const string WarehouseIconPath = "res://assets/ui/map/icons/warehouse-icon.png";
	private const string RefineryIconPath = "res://assets/ui/map/icons/refinery-icon.png";
	private const string TravelIconPath = "res://assets/ui/map/icons/travel-icon.png";
	private const string MarketIconPath = "res://assets/ui/map/icons/market-icon.png";
	private const string MineIconPath = "res://assets/ui/map/services/copper-mine-manager.png";
	private static readonly Color DockyardIconTint = new(0.45f, 0.65f, 1f);
	private const int IconPx = 40;

	private static readonly HashSet<string> AllowedSources = new(StringComparer.Ordinal)
	{
		CinematicPresentationMode.ModeId,
	};

	private static readonly PresentationInputPolicy Policy = new(
		AllowsOrbit: true,
		AllowsPan: false,
		AllowsWheelZoom: true,
		AllowsMapMovement: false,
		AllowsStrategicHover: false);

	private static readonly OrbitLimits ModeLimits = new(
		MinDistance: 2f,
		MaxDistance: 7f,
		MinPitch: Mathf.DegToRad(15f),
		MaxPitch: Mathf.DegToRad(45f));

	private readonly CanvasLayer _uiLayer;
	private readonly ColorRect _fadeOverlay;
	private readonly Func<PresentationTransitionResult> _requestExit;

	private readonly List<Button> _facilityButtons = [];
	private PointOfInterest? _activePoi;
	private FacadeEnterPayload? _pendingPayload;
	private MapPresentationContext? _ctx;
	private bool _enteringFacility;

	public FacadePresentationMode(
		CanvasLayer uiLayer,
		ColorRect fadeOverlay,
		Func<PresentationTransitionResult> requestExit)
	{
		_uiLayer = uiLayer;
		_fadeOverlay = fadeOverlay;
		_requestExit = requestExit;
	}

	public event Action<FacilityEntry>? FacilityEntered;

	public string Id => ModeId;
	public OrbitLimits Limits => ModeLimits;
	public PresentationInputPolicy InputPolicy => Policy;
	public IReadOnlySet<string> AllowedFrom => AllowedSources;
	public string? ExitTargetId => CinematicPresentationMode.ModeId;
	public bool IsBusy => _enteringFacility;

	public PresentationTransitionResult ValidateEnterPayload(object? payload) =>
		payload is FacadeEnterPayload
			? PresentationTransitionResult.Ok()
			: PresentationTransitionResult.Fail(PresentationTransitionFailure.InvalidPayload);

	public bool CanEnter(MapPresentationContext ctx, string sourceModeId, object? payload)
	{
		if (payload is not FacadeEnterPayload facadePayload)
			return false;

		if (!ctx.CanAccessFacilities())
			return false;

		var dockedPoiId = ctx.ResolveDockedPoiId();
		return dockedPoiId is not null && dockedPoiId == facadePayload.PoiId;
	}

	public OrbitPose ResolveEnterPose(
		string sourceModeId,
		MapPresentationContext ctx,
		object? payload)
	{
		var poi = ResolvePoi(ctx, payload);
		var world = ctx.Map();
		var pose = ctx.View.ResolveFacadePose(poi, world.Width, world.Height);
		pose.Distance = MapZoomNavigation.ClampSavedDistanceToInterior(pose.Distance, ModeLimits);
		return pose;
	}

	public void OnEntering(MapPresentationContext ctx, string sourceModeId, object? payload)
	{
		_ctx = ctx;
		_pendingPayload = payload as FacadeEnterPayload;
		_activePoi = null;

		if (string.IsNullOrEmpty(sourceModeId))
			RestoreStrategicCameraPose(ctx);
	}

	public void OnSettled(MapPresentationContext ctx)
	{
		_ctx = ctx;
		var poi = ResolveActivePoi(ctx);
		_activePoi = poi;
		_pendingPayload = null;
		ctx.Camera.SetFacadeActive(true);
		CreateFacilityButtons(poi);
	}

	public void OnExiting(MapPresentationContext ctx, string targetModeId)
	{
		_ctx = null;
		_pendingPayload = null;
		_activePoi = null;
		_enteringFacility = false;
		ctx.Camera.SetFacadeActive(false);
		ClearFacilityButtons();
	}

	public void Update(MapPresentationContext ctx, double delta)
	{
		_ctx = ctx;
		if (_enteringFacility)
			return;

		if (ctx.ResolveDockedPoiId() is null)
		{
			_requestExit();
			return;
		}

		if (_activePoi is not null)
			PositionFacilityButtons(_activePoi, ctx);
	}

	public bool FilterInput(InputEvent @event)
	{
		if (_enteringFacility)
			return true;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return true;

		return @event is InputEventMouseButton { ButtonIndex: MouseButton.Left };
	}

	private PointOfInterest ResolveActivePoi(MapPresentationContext ctx)
	{
		if (_activePoi is not null)
			return _activePoi;

		return ResolvePoi(ctx, _pendingPayload);
	}

	private static PointOfInterest ResolvePoi(MapPresentationContext ctx, object? payload)
	{
		if (payload is not FacadeEnterPayload facadePayload)
			throw new InvalidOperationException("Facade mode requires FacadeEnterPayload.");

		var world = ctx.Map();
		return world.PointsOfInterest.First(p => p.Id == facadePayload.PoiId);
	}

	private void BeginEnterFacility(PointOfInterest poi, Facility facility, MapPresentationContext ctx)
	{
		if (_enteringFacility || !ctx.CanAccessFacilities())
			return;

		_enteringFacility = true;
		SetFacilityButtonsVisible(false);

		var world = ctx.Map();
		var anchor = ctx.View.ResolveFacilityAnchorWorldPosition(
			poi,
			facility.PresentationAnchor,
			world.Width,
			world.Height);
		var current = ctx.Camera.CurrentPose;
		var target = new OrbitPose
		{
			Pivot = anchor,
			Yaw = current.Yaw,
			Pitch = current.Pitch,
			Distance = FacilityZoomDistance,
		};
		ctx.TweenToPose(target, Limits, () => FadeToBlack(() =>
			FacilityEntered?.Invoke(new FacilityEntry(poi.Id, facility))));
	}

	private void FadeToBlack(Action onComplete)
	{
		_uiLayer.MoveChild(_fadeOverlay, -1);
		_fadeOverlay.Visible = true;
		_fadeOverlay.Color = new Color(0f, 0f, 0f, 0f);
		_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Stop;

		var tween = _uiLayer.CreateTween();
		tween.TweenProperty(_fadeOverlay, "color", Colors.Black, FacilityFadeDuration);
		tween.TweenCallback(Callable.From(onComplete));
	}

	private void CreateFacilityButtons(PointOfInterest poi)
	{
		ClearFacilityButtons();

		foreach (var facility in poi.Facilities)
		{
			var button = new Button
			{
				TooltipText = facility.DisplayName,
				Visible = true,
				MouseFilter = Control.MouseFilterEnum.Stop,
				Flat = true,
				ThemeTypeVariation = "MapIcon",
				Icon = LoadFacilityIcon(facility.PresentationAnchor),
				ExpandIcon = true,
				CustomMinimumSize = new Vector2(48, 48),
			};
			button.Pressed += () =>
			{
				if (_activePoi is null || _ctx is null)
					return;

				BeginEnterFacility(_activePoi, facility, _ctx);
			};
			_uiLayer.AddChild(button);
			_facilityButtons.Add(button);
		}
	}

	private void PositionFacilityButtons(PointOfInterest poi, MapPresentationContext ctx)
	{
		var viewport = ctx.ViewportSize();
		var world = ctx.Map();
		for (var i = 0; i < _facilityButtons.Count; i++)
		{
			var facility = poi.Facilities[i];
			var worldPos = ctx.View.ResolveFacilityAnchorWorldPosition(
				poi,
				facility.PresentationAnchor,
				world.Width,
				world.Height);
			var screen = ctx.Camera.UnprojectPosition(worldPos);
			var button = _facilityButtons[i];
			button.ResetSize();
			var buttonSize = button.Size;
			var position = screen - buttonSize * 0.5f;
			position.X = Mathf.Clamp(position.X, 8f, viewport.Width - buttonSize.X - 8f);
			position.Y = Mathf.Clamp(position.Y, 8f, viewport.Height - buttonSize.Y - 8f);
			button.Position = position;
		}
	}

	private void SetFacilityButtonsVisible(bool visible)
	{
		foreach (var button in _facilityButtons)
			button.Visible = visible;
	}

	private void ClearFacilityButtons()
	{
		foreach (var button in _facilityButtons)
			button.QueueFree();
		_facilityButtons.Clear();
	}

	private static Texture2D LoadFacilityIcon(EPresentationAnchor anchor) =>
		anchor switch
		{
			EPresentationAnchor.Management => SvgIconLoader.LoadRaw(ManagementIconPath, IconPx),
			EPresentationAnchor.Dockyard => SvgIconLoader.Load(DockyardIconPath, DockyardIconTint, IconPx),
			EPresentationAnchor.Warehouse => SvgIconLoader.LoadRaw(WarehouseIconPath, IconPx),
			EPresentationAnchor.Refinery => SvgIconLoader.LoadRaw(RefineryIconPath, IconPx),
			EPresentationAnchor.Travel => SvgIconLoader.LoadRaw(TravelIconPath, IconPx),
			EPresentationAnchor.Market => SvgIconLoader.LoadRaw(MarketIconPath, IconPx),
			EPresentationAnchor.Mine => SvgIconLoader.LoadRaw(MineIconPath, IconPx),
			_ => SvgIconLoader.LoadRaw(ManagementIconPath, IconPx),
		};

	private static void RestoreStrategicCameraPose(MapPresentationContext ctx)
	{
		if (MapNavigationContext.StrategicCameraPose is not { } saved)
			return;

		ctx.Camera.SetCapturedPose(saved);
	}
}
