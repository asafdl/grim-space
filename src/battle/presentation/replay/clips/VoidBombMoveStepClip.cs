using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class VoidBombMoveStepClip : IReplayClip
{
	public Type ActionType => typeof(VoidBombMoveStepAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var move = (VoidBombMoveStepAction)action;
		context.ReplayState.ApplyVoidBombMove(move);
		var state = context.ReplayState.StateOf(move.ActorId);

		context.UnitViews[move.ActorId].AnimateMoveTo(
			state,
			ReplayTiming.MoveStepSeconds,
			context.NextMovePosition(move.ActorId));
		return ClipPlayback.Pause(ReplayTiming.MoveStepSeconds);
	}
}
