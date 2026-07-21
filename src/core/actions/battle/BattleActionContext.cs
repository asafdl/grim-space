using GrimSpace.Battle.Slices;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions.Battle;

public sealed class BattleActionContext : ActionContext<BattleSlices>
{
	private BattleActionContext(BattleBoard board, TurnState turnState, string actorId)
	{
		Board = board;
		TurnState = turnState;
		ActorId = actorId;
	}

	public BattleBoard Board { get; }

	public TurnState TurnState { get; }

	public string ActorId { get; }

	public override BattleSlices Slices =>
		SystemAction.Is(ActorId)
			? BattleSlices.ForSystem(Board)
			: BattleSlices.For(Board, ActorId, TurnState);

	public static BattleActionContext For(BattleBoard board, TurnState turnState, string actorId) =>
		new(board, turnState, actorId);
}
