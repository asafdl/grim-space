using GrimSpace.Battle;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Tests.Simulation;

internal static class Preview
{
	public static PreviewActor Simulate(BattleOrchestrator battle) =>
		new(battle.Sim.StateOf<ActorState>(battle.PlayerId));

	public static IReadOnlyList<Option> GetLegalMoves(BattleOrchestrator battle) =>
		battle.GetActiveActor() is { } actor && battle.CanAct(actor)
			? BattleTestFixture.Ui(battle).MoveUi.GetMoveOptions(battle.Sim.Actions)
			: [];
}

internal readonly record struct PreviewActor(State Actor);
