using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests;

internal static class BattleSpawnTestKit
{
	public static BattleSpawn Create(
		string id,
		EType chassis,
		ETeam team,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent) =>
		new()
		{
			Ship = ShipCatalog.DefaultSnapshot(id, chassis),
			Team = team,
			Position = position,
			ExecutionAgent = executionAgent,
		};
}
