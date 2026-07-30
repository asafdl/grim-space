using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class ResolveHazardClip : IReplayClip
{
	private const double DisplaySeconds = 0.25;

	public Type ActionType => typeof(ResolveHazardAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var resolve = (ResolveHazardAction)action;
		foreach (var unitId in context.ReplayState.ApplyResolveHazard(resolve))
			context.SyncUnit(unitId);

		context.HazardBursts.RecordBurst(resolve.Kind, resolve.Cells);
		return ClipPlayback.Pause(DisplaySeconds);
	}
}
