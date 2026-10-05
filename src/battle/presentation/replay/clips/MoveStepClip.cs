using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class MoveStepClip : IReplayClip
{

	public Type ActionType => typeof(MoveStepAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var move = (MoveStepAction)action;
		context.ReplayState.ApplyMove(move);
		var state = context.ReplayState.StateOf(move.ActorId);

		context.UnitViews[move.ActorId].AnimatePoseTo(
			state,
			ReplayTiming.MoveStepSeconds,
			context.NextMovePosition(move.ActorId));
		return ClipPlayback.Pause(ReplayTiming.MoveStepSeconds);
	}
}
