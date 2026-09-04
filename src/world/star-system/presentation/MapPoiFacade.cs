using Godot;
using GrimSpace.Presentation.Ui;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// POI facade camera transitions and dock access icon. Presentation-only.
/// </summary>
public sealed class MapPoiFacade
{
	private enum FacadeState
	{
		Strategic,
		Entering,
		Facade,
		Exiting,
	}

	private const float EnterDistance = 12f;
	private const float ExitDistance = 17f;
	private const float PivotProximity = 2.5f;
	private const float TweenDuration = 0.45f;
	private const string AccessIconPath = "res://assets/ui/map/dock-facilities.svg";
	private const string ManagementIconPath = "res://assets/ui/map/management-facility.svg";
	private const int IconPx = 40;

	private readonly MapView _view;
	private readonly MapCamera _camera;
	private readonly Func<StarMap> _map;
	private readonly Func<Vector2> _viewportSize;

	private CanvasLayer? _uiLayer;
	private Button _accessButton = null!;
	private PointOfInterest? _activePoi;
	private readonly List<Button> _facilityButtons = [];
	private FacadeState _state = FacadeState.Strategic;

	public MapPoiFacade(
		MapView view,
		MapCamera camera,
		Func<StarMap> map,
		Func<Vector2> viewportSize)
	{
		_view = view;
		_camera = camera;
		_map = map;
		_viewportSize = viewportSize;
	}

	public bool IsStrategic => _state == FacadeState.Strategic;

	public void BuildUi(CanvasLayer layer)
	{
		_uiLayer = layer;
		_accessButton = new Button
		{
			TooltipText = "View local facilities",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Stop,
			Flat = true,
			Icon = SvgIconLoader.LoadRaw(AccessIconPath, IconPx),
			ExpandIcon = true,
			CustomMinimumSize = new Vector2(48, 48),
		};
		_accessButton.AddThemeStyleboxOverride("normal", IconButtonStyle(0f));
		_accessButton.AddThemeStyleboxOverride("hover", IconButtonStyle(0.15f));
		_accessButton.AddThemeStyleboxOverride("pressed", IconButtonStyle(0.25f));
		_accessButton.Pressed += OnAccessButtonPressed;
		layer.AddChild(_accessButton);
	}

	public void Update()
	{
		var world = _map();
		var dockedPoiId = ResolveDockedPoiId(world);
		var dockedPoi = dockedPoiId is not null
			? world.PointsOfInterest.First(p => p.Id == dockedPoiId)
			: null;

		if (dockedPoiId is null && _state is FacadeState.Facade or FacadeState.Entering)
		{
			BeginExit();
			return;
		}

		var showAccess = _state == FacadeState.Strategic && dockedPoiId is not null;
		_accessButton.Visible = showAccess;
		if (showAccess && dockedPoiId is not null)
			PositionAccessButton(dockedPoiId, world);

		if (_state == FacadeState.Strategic
			&& dockedPoi is not null
			&& _camera.Distance <= EnterDistance
			&& IsPivotNearPoi(_camera.CurrentPose.Pivot, dockedPoi, world))
		{
			BeginEnter(dockedPoi, world);
			return;
		}

		if (_state == FacadeState.Facade
			&& !_camera.IsAnimating
			&& _camera.Distance >= ExitDistance)
		{
			BeginExit();
		}

		if (_state == FacadeState.Facade && _activePoi is not null)
			PositionFacilityButtons(_activePoi, world);
	}

	/// <summary>
	/// Returns true when the event should not reach map controls (handled or blocked).
	/// </summary>
	public bool FilterInput(InputEvent @event)
	{
		if (_state is not (FacadeState.Facade or FacadeState.Entering or FacadeState.Exiting))
			return false;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			if (_state == FacadeState.Facade)
				BeginExit();
			return true;
		}

		return @event is InputEventMouseButton { ButtonIndex: MouseButton.Right };
	}

	private void OnAccessButtonPressed()
	{
		var world = _map();
		var dockedPoiId = ResolveDockedPoiId(world);
		if (dockedPoiId is null)
			return;

		var poi = world.PointsOfInterest.First(p => p.Id == dockedPoiId);
		BeginEnter(poi, world);
	}

	private void PositionAccessButton(string dockedPoiId, StarMap world)
	{
		var worldPos = _view.GetDockWorldPosition(dockedPoiId, world.Width, world.Height)
			+ new Vector3(0.22f, 0.28f, 0f);
		var screen = _camera.UnprojectPosition(worldPos);
		_accessButton.ResetSize();
		var viewport = _viewportSize();
		var buttonSize = _accessButton.Size;
		var position = screen + new Vector2(8f, -buttonSize.Y * 0.5f);
		position.X = Mathf.Clamp(position.X, 8f, viewport.X - buttonSize.X - 8f);
		position.Y = Mathf.Clamp(position.Y, 8f, viewport.Y - buttonSize.Y - 8f);
		_accessButton.Position = position;
	}

	private void BeginEnter(PointOfInterest poi, StarMap world)
	{
		if (_state != FacadeState.Strategic)
			return;

		_state = FacadeState.Entering;
		_accessButton.Visible = false;
		_camera.CapturePose();
		var target = _view.ResolveFacadePose(poi, world.Width, world.Height);
		_camera.TweenToPose(target, TweenDuration, () => OnEnterComplete(poi));
	}

	private void OnEnterComplete(PointOfInterest poi)
	{
		_state = FacadeState.Facade;
		_camera.SetFacadeActive(true);
		_activePoi = poi;
		CreateFacilityButtons(poi);
	}

	private void BeginExit()
	{
		if (_state is FacadeState.Strategic or FacadeState.Exiting)
			return;

		_state = FacadeState.Exiting;
		_camera.SetFacadeActive(false);
		ClearFacilityButtons();
		_activePoi = null;
		_camera.RestoreCapturedPose(TweenDuration, () => _state = FacadeState.Strategic, ExitDistance);
	}

	private void CreateFacilityButtons(PointOfInterest poi)
	{
		ClearFacilityButtons();
		if (_uiLayer is null)
			return;

		foreach (var facility in poi.Facilities)
		{
			var button = new Button
			{
				TooltipText = facility.DisplayName,
				Visible = true,
				MouseFilter = Control.MouseFilterEnum.Stop,
				Flat = true,
				Icon = SvgIconLoader.LoadRaw(ResolveFacilityIconPath(facility.PresentationAnchor), IconPx),
				ExpandIcon = true,
				CustomMinimumSize = new Vector2(48, 48),
			};
			button.AddThemeStyleboxOverride("normal", IconButtonStyle(0f));
			button.AddThemeStyleboxOverride("hover", IconButtonStyle(0.15f));
			button.AddThemeStyleboxOverride("pressed", IconButtonStyle(0.25f));
			_uiLayer.AddChild(button);
			_facilityButtons.Add(button);
		}
	}

	private void PositionFacilityButtons(PointOfInterest poi, StarMap world)
	{
		var viewport = _viewportSize();
		for (var i = 0; i < _facilityButtons.Count; i++)
		{
			var facility = poi.Facilities[i];
			var worldPos = _view.ResolveFacilityAnchorWorldPosition(
				poi,
				facility.PresentationAnchor,
				world.Width,
				world.Height);
			var screen = _camera.UnprojectPosition(worldPos);
			var button = _facilityButtons[i];
			button.ResetSize();
			var buttonSize = button.Size;
			var position = screen - buttonSize * 0.5f;
			position.X = Mathf.Clamp(position.X, 8f, viewport.X - buttonSize.X - 8f);
			position.Y = Mathf.Clamp(position.Y, 8f, viewport.Y - buttonSize.Y - 8f);
			button.Position = position;
		}
	}

	private void ClearFacilityButtons()
	{
		foreach (var button in _facilityButtons)
			button.QueueFree();
		_facilityButtons.Clear();
	}

	private static string ResolveFacilityIconPath(EPresentationAnchor anchor) =>
		anchor switch
		{
			EPresentationAnchor.Management => ManagementIconPath,
			_ => ManagementIconPath,
		};

	private static string? ResolveDockedPoiId(StarMap world)
	{
		var player = world.UnitRegistry.UnitOf(Run.State.PlayerFleetUnitId);
		if (player.State.Phase != EPhase.Docked || string.IsNullOrEmpty(player.State.DockedAtDockId))
			return null;

		return world.DocksById[player.State.DockedAtDockId].PoiId;
	}

	private bool IsPivotNearPoi(Vector3 pivot, PointOfInterest poi, StarMap world)
	{
		var poiCenter = _view.GetPoiWorldPosition(poi.Id, world.Width, world.Height);
		var dx = pivot.X - poiCenter.X;
		var dz = pivot.Z - poiCenter.Z;
		return Mathf.Sqrt(dx * dx + dz * dz) <= PivotProximity;
	}

	private static StyleBoxFlat IconButtonStyle(float bgAlpha) => new()
	{
		BgColor = new Color(0.02f, 0.04f, 0.07f, bgAlpha),
		CornerRadiusTopLeft = 2,
		CornerRadiusTopRight = 2,
		CornerRadiusBottomLeft = 2,
		CornerRadiusBottomRight = 2,
		ContentMarginLeft = 0,
		ContentMarginRight = 0,
		ContentMarginTop = 0,
		ContentMarginBottom = 0,
	};
}
