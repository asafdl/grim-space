using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class OverviewPresentationMode : IPresentationMode
{
	public const string ModeId = "overview";

	private static readonly HashSet<string> AllowedSources = new(StringComparer.Ordinal)
	{
		CinematicPresentationMode.ModeId,
	};

	private static readonly PresentationInputPolicy Policy = new(
		AllowsOrbit: true,
		AllowsPan: true,
		AllowsWheelZoom: true,
		AllowsMapMovement: true,
		AllowsStrategicHover: true);

	private OrbitLimits _limits = new(22f, 22f, 0.87f, 1.13f);

	public string Id => ModeId;
	public OrbitLimits Limits => _limits;
	public PresentationInputPolicy InputPolicy => Policy;
	public IReadOnlySet<string> AllowedFrom => AllowedSources;
	public string? ExitTargetId => CinematicPresentationMode.ModeId;
	public bool IsBusy => false;

	public PresentationTransitionResult ValidateEnterPayload(object? payload) =>
		PresentationTransitionResult.Ok();

	public bool CanEnter(MapPresentationContext ctx, string sourceModeId, object? payload) => true;

	public OrbitPose ResolveEnterPose(
		string sourceModeId,
		MapPresentationContext ctx,
		object? payload)
	{
		RefreshLimits(ctx);
		var viewport = ctx.ViewportSize();
		return MapOverviewFraming.Resolve(
			ctx.Camera.CurrentPose,
			ctx.BoundsHalfX,
			ctx.BoundsHalfZ,
			viewport.Width,
			viewport.Height);
	}

	public void OnEntering(MapPresentationContext ctx, string sourceModeId, object? payload) =>
		RefreshLimits(ctx);

	public void OnSettled(MapPresentationContext ctx) { }

	public void OnExiting(MapPresentationContext ctx, string targetModeId) { }

	public void Update(MapPresentationContext ctx, double delta) { }

	private void RefreshLimits(MapPresentationContext ctx)
	{
		var viewport = ctx.ViewportSize();
		_limits = MapOverviewFraming.ResolveLimits(
			ctx.BoundsHalfX,
			ctx.BoundsHalfZ,
			viewport.Width,
			viewport.Height);
	}
}
