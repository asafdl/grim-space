using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Movement;

public static class MovePathEndpoints
{
	public static IReadOnlyList<MovePathSession> DiscoverExtensions(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId) =>
		MovePathIndex.Build(sim, actorId).GetExtensions([]);
}
