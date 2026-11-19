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

		var seconds = context.Seconds(ReplayTiming.MoveStepSeconds);
		context.UnitViews[move.ActorId].AnimatePoseTo(
			state,
			seconds,
			context.NextMovePosition(move.ActorId));
		if (state.IsAlive)
			context.UnitViews[move.ActorId].PlayMovementSound(seconds);
		return ClipPlayback.Pause(seconds);
	}
}
