using GrimSpace.Battle;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Units;

namespace GrimSpace.Tests.Simulation;

internal static class Preview
{
	public static PreviewActor Simulate(BattleOrchestrator battle) =>
		new(battle.Sim.StateOf<ActorState>(battle.PlayerId));

	public static IReadOnlyList<Option> GetLegalMoves(BattleOrchestrator battle) =>
		MoveUi.GetMoveOptions(battle, battle.GetActiveActor());
}

internal readonly record struct PreviewActor(State Actor);
