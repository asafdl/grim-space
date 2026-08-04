using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;

namespace GrimSpace.Tests;

internal static class SimulationPreviewShim
{
	public static IReadOnlyList<MovePathSession> GetLegalMoves(BattleOrchestrator battle) =>
		battle.Sim.Actions.Count == 0
			|| battle.Sim.Actions[^1] is not FlakAction and not RailgunAction
			? BattleTestFixture.Ui(battle).MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions)
			: [];
}
