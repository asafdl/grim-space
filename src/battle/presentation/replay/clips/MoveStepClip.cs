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
		var from = context.ReplayState.StateOf(move.ActorId).Position;
		context.ReplayState.ApplyMove(move);
		var state = context.ReplayState.StateOf(move.ActorId);

		context.UnitViews[move.ActorId].AnimateMoveTo(state, ReplayTiming.MoveStepSeconds);
		context.TurnHistory.RecordMove(move.ActorId, from, state.Position, context.ColorFor(move.ActorId));
		return ClipPlayback.Pause(ReplayTiming.MoveStepSeconds);
	}
}
