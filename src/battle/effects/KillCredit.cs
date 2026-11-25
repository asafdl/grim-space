using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;

namespace GrimSpace.Battle.Effects;

public static class KillCredit
{
	public static IEnumerable<State> Credited(BattleWorld world, ImpactFacts impact)
	{
		var target = world.UnitRegistry.UnitOf(impact.TargetId);
		var visited = new HashSet<string>(StringComparer.Ordinal);
		var creditedId = impact.SourceId;
		while (visited.Add(creditedId) && world.UnitRegistry.TryGet(creditedId, out var credited))
		{
			if (credited.State.IsAlive && credited.RelationTo(target) == EUnitRelation.Opponent)
				yield return credited.State;
			creditedId = credited.State.ParentId;
		}
	}
}
