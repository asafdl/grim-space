using GrimSpace.Battle.Board;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Turn;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Actions;

public sealed class BattleActionContext : ActionContext<BattleSlices>
{
	private BattleActionContext(BattleBoard board, TurnPhaseContext phaseContext, string actorId)
	{
		Board = board;
		PhaseContext = phaseContext;
		ActorId = actorId;
	}

	public BattleBoard Board { get; }

	public TurnPhaseContext PhaseContext { get; }

	public string ActorId { get; }

	public override BattleSlices Slices =>
		SystemAction.Is(ActorId)
			? BattleSlices.ForSystem(Board)
			: BattleSlices.For(Board, ActorId, PhaseContext);

	public static BattleActionContext For(BattleBoard board, TurnPhaseContext phaseContext, string actorId) =>
		new(board, phaseContext, actorId);
}
