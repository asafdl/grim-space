using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;

namespace GrimSpace.Tests;

internal static class SimulationPreviewShim
{
	public static IReadOnlyList<MovePathSession> GetLegalMoves(BattleOrchestrator battle) =>
		battle.PlayerAgent.Sim.Actions.Count == 0
			|| battle.PlayerAgent.Sim.Actions[^1] is not FlakAction and not RailgunAction
			? MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			: [];
}
