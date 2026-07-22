using GrimSpace.Battle;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Planning;
using GrimSpace.Battle.Units;

namespace GrimSpace.Tests.Planning;

internal static class Preview
{
	public static PreviewActor Simulate(BattleOrchestrator battle) =>
		new(battle.Board.StateOf(battle.OwnerId));

	public static IReadOnlyList<Option> GetLegalMoves(BattleOrchestrator battle) =>
		View.GetLegalMoves(battle).ToList();
}

internal readonly record struct PreviewActor(State Actor);
