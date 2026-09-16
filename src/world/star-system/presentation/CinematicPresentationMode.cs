using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class CinematicPresentationMode : IPresentationMode
{
	public const string ModeId = "cinematic";
	private const float FollowResponse = 0.35f;
	private const float RecenterResponse = 1.5f;
	private const float MaxPanOffset = 2.5f;

	private static readonly HashSet<string> AllowedSources = new(StringComparer.Ordinal)
	{
		OverviewPresentationMode.ModeId,
		FacadePresentationMode.ModeId,
	};

	private static readonly PresentationInputPolicy Policy = new(
		AllowsOrbit: true,
		AllowsPan: true,
		AllowsWheelZoom: true,
		AllowsRmbMovement: true,
		AllowsStrategicHover: true);

	private static readonly OrbitLimits ModeLimits = new(
		MinDistance: 10f,
		MaxDistance: 14f,
		MinPitch: Mathf.DegToRad(20f),
		MaxPitch: Mathf.DegToRad(40f));

	private readonly Button _accessButton;
	private OrbitPose _savedPose;
	private bool _hasSavedPose;

	public CinematicPresentationMode(Button accessButton) => _accessButton = accessButton;

	public string Id => ModeId;
	public OrbitLimits Limits => ModeLimits;
	public PresentationInputPolicy InputPolicy => Policy;
	public IReadOnlySet<string> AllowedFrom => AllowedSources;
	public string? ExitTargetId => null;
	public bool IsBusy => false;

	public PresentationTransitionResult ValidateEnterPayload(object? payload) =>
		payload is null
			? PresentationTransitionResult.Ok()
			: PresentationTransitionResult.Fail(PresentationTransitionFailure.InvalidPayload);

	public bool CanEnter(MapPresentationContext ctx, string sourceModeId, object? payload) => true;

	public OrbitPose ResolveEnterPose(
		string sourceModeId,
		MapPresentationContext ctx,
		object? payload)
	{
		if (sourceModeId == OverviewPresentationMode.ModeId)
		{
			var sample = ctx.ResolvePlayerTravelSample();
			if (sample.TravelDirection is { } direction)
			{
				return MapCinematicFraming.BehindShip(
					sample.WorldPosition,
					direction,
					ModeLimits);
			}
		}

		if (_hasSavedPose)
			return _savedPose;

		if (sourceModeId == string.Empty)
			return MapCinematicFraming.BootstrapAtPlayer(ctx.ResolvePlayerTravelSample(), ModeLimits);

		return ctx.Camera.CurrentPose;
	}

	public void OnEntering(MapPresentationContext ctx, string sourceModeId, object? payload)
	{
		_accessButton.Visible = false;
		if (sourceModeId == FacadePresentationMode.ModeId)
			MapNavigationContext.ClearStrategicCameraPose();
	}

	public void OnSettled(MapPresentationContext ctx) { }

	public void OnExiting(MapPresentationContext ctx, string targetModeId)
	{
		_savedPose = ctx.Camera.CurrentPose;
		_hasSavedPose = true;
		_accessButton.Visible = false;

		if (targetModeId == FacadePresentationMode.ModeId)
			MapNavigationContext.SaveStrategicCameraPose(_savedPose);
	}

	public void Update(MapPresentationContext ctx, double delta)
	{
		FollowPlayer(ctx, delta);

		var world = ctx.Map();
		var dockedPoiId = ctx.ResolveDockedPoiId();
		var showAccess = dockedPoiId is not null && ctx.CanAccessFacilities();
		_accessButton.Visible = showAccess;
		if (!showAccess || dockedPoiId is null)
			return;

		var worldPos = ctx.View.GetDockWorldPosition(dockedPoiId, world.Width, world.Height);
		if (!MapScreenAnchor.TryProject(ctx.Camera, worldPos, out var screen))
		{
			_accessButton.Visible = false;
			return;
		}

		_accessButton.ResetSize();
		var buttonSize = _accessButton.Size;
		_accessButton.Position = MapScreenAnchor.TopLeftForControl(
			screen,
			buttonSize,
			new Vector2(8f, 0f));
	}

	private static void FollowPlayer(MapPresentationContext ctx, double delta)
	{
		var camera = ctx.Camera;
		if (camera.IsAnimating)
			return;

		var playerPos = ctx.ResolvePlayerTravelSample().WorldPosition;

		if (!camera.IsManualGestureActive)
		{
			camera.MovePivotToward(playerPos, (float)delta, FollowResponse);
			return;
		}

		var pose = camera.CurrentPose;
		var offset = new Vector3(pose.Pivot.X - playerPos.X, 0f, pose.Pivot.Z - playerPos.Z);
		if (offset.Length() > MaxPanOffset)
			offset = offset.Normalized() * MaxPanOffset;

		var recenterT = Mathf.Clamp((float)delta / RecenterResponse, 0f, 1f);
		offset *= 1f - recenterT;
		camera.MovePivotToward(playerPos + offset, (float)delta, FollowResponse);
	}
}
