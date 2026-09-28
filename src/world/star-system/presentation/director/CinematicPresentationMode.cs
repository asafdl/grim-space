using Godot;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.Math.Camera;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Director;

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
		AllowsMapMovement: true,
		AllowsStrategicHover: true);

	private static readonly OrbitLimits ModeLimits = new(
		MinDistance: 9f,
		MaxDistance: 18f,
		MinPitch: Mathf.DegToRad(20f),
		MaxPitch: Mathf.DegToRad(40f));

	private OrbitPose _savedPose;
	private bool _hasSavedPose;

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
			var interiorDistance = MapZoomNavigation.InteriorDistance(ModeLimits, fromMinSide: true);
			if (sample.TravelDirection is { } direction)
			{
				return MapCinematicFraming.BehindShip(
					sample.WorldPosition,
					direction,
					ModeLimits,
					interiorDistance);
			}
		}

		if (_hasSavedPose)
		{
			_savedPose.Distance = MapZoomNavigation.ClampSavedDistanceToInterior(
				_savedPose.Distance,
				ModeLimits);
			return _savedPose;
		}

		if (sourceModeId == string.Empty)
			return MapCinematicFraming.BootstrapAtPlayer(ctx.ResolvePlayerTravelSample(), ModeLimits);

		if (sourceModeId == FacadePresentationMode.ModeId)
		{
			var pose = ctx.Camera.CurrentPose;
			pose.Distance = MapZoomNavigation.InteriorDistance(ModeLimits, fromMinSide: false);
			return pose;
		}

		return ctx.Camera.CurrentPose;
	}

	public void OnEntering(MapPresentationContext ctx, string sourceModeId, object? payload)
	{
		if (sourceModeId == FacadePresentationMode.ModeId)
			MapNavigationContext.ClearStrategicCameraPose();
	}

	public void OnSettled(MapPresentationContext ctx) { }

	public void OnExiting(MapPresentationContext ctx, string targetModeId)
	{
		_savedPose = ctx.Camera.CurrentPose;
		if (targetModeId is FacadePresentationMode.ModeId or OverviewPresentationMode.ModeId)
		{
			_savedPose.Distance = MapZoomNavigation.ClampSavedDistanceToInterior(
				_savedPose.Distance,
				ModeLimits);
		}

		_hasSavedPose = true;

		if (targetModeId == FacadePresentationMode.ModeId)
			MapNavigationContext.SaveStrategicCameraPose(_savedPose);
	}

	public void Update(MapPresentationContext ctx, double delta) => FollowPlayer(ctx, delta);

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
