using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class CinematicPresentationMode : IPresentationMode
{
	public const string ModeId = "cinematic";

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
		PresentationTransitionResult.Ok();

	public bool CanEnter(MapPresentationContext ctx, string sourceModeId, object? payload) => true;

	public OrbitPose ResolveEnterPose(
		string sourceModeId,
		MapPresentationContext ctx,
		object? payload) =>
		_hasSavedPose ? _savedPose : ctx.Camera.CurrentPose;

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
		var world = ctx.Map();
		var dockedPoiId = ctx.ResolveDockedPoiId();
		var showAccess = dockedPoiId is not null && ctx.CanAccessFacilities();
		_accessButton.Visible = showAccess;
		if (!showAccess || dockedPoiId is null)
			return;

		var worldPos = ctx.View.GetDockWorldPosition(dockedPoiId, world.Width, world.Height)
			+ new Vector3(0.22f, 0.28f, 0f);
		var screen = ctx.Camera.UnprojectPosition(worldPos);
		_accessButton.ResetSize();
		var viewport = ctx.ViewportSize();
		var buttonSize = _accessButton.Size;
		var position = screen + new Vector2(8f, -buttonSize.Y * 0.5f);
		position.X = Mathf.Clamp(position.X, 8f, viewport.Width - buttonSize.X - 8f);
		position.Y = Mathf.Clamp(position.Y, 8f, viewport.Height - buttonSize.Y - 8f);
		_accessButton.Position = position;
	}
}
