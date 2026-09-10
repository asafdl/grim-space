using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class TorpedoMoveStepClip : IReplayClip
{
	public Type ActionType => typeof(TorpedoMoveStepAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var move = (TorpedoMoveStepAction)action;
		var from = context.ReplayState.StateOf(move.ActorId).Position;
		context.ReplayState.ApplyTorpedoMove(move);
		var state = context.ReplayState.StateOf(move.ActorId);

		context.UnitViews[move.ActorId].AnimateMoveTo(state, ReplayTiming.MoveStepSeconds);
		context.TurnHistory.RecordMove(move.ActorId, from, state.Position, context.ColorFor(move.ActorId));
		return ClipPlayback.Pause(ReplayTiming.MoveStepSeconds);
	}
}
