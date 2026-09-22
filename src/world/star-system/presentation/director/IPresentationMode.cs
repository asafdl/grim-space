using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation.Director;

public interface IPresentationMode
{
	string Id { get; }
	OrbitLimits Limits { get; }
	PresentationInputPolicy InputPolicy { get; }

	IReadOnlySet<string> AllowedFrom { get; }
	string? ExitTargetId { get; }

	PresentationTransitionResult ValidateEnterPayload(object? payload);

	bool CanEnter(MapPresentationContext ctx, string sourceModeId, object? payload);

	OrbitPose ResolveEnterPose(
		string sourceModeId,
		MapPresentationContext ctx,
		object? payload);

	void OnEntering(MapPresentationContext ctx, string sourceModeId, object? payload);
	void OnSettled(MapPresentationContext ctx);
	void OnExiting(MapPresentationContext ctx, string targetModeId);

	void Update(MapPresentationContext ctx, double delta);

	bool IsBusy { get; }
}
