using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class MoveStepClip : IReplayClip
{
	private const double DurationSeconds = 0.01;

	public Type ActionType => typeof(MoveStepAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var move = (MoveStepAction)action;
		var from = context.ReplayState.Position(move.ActorId);
		context.ReplayState.ApplyMove(move);
		var to = context.ReplayState.Position(move.ActorId);

		context.SyncUnit(move.ActorId);
		context.TurnHistory.RecordMove(move.ActorId, from, to, context.ColorFor(move.ActorId));
		return ClipPlayback.Pause(DurationSeconds);
	}
}
