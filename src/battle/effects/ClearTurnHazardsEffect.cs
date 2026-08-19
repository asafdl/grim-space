using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Ids;
using GrimSpace.Core;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ClearTurnHazardsEffect : IEffect<BattleWorld, ActorRuntime>
{
	private List<Hazard> _removed = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_removed = world.NonUnits.Values
			.Where(nonUnit => nonUnit.ActorId != BattleActorIds.Terrain)
			.OfType<Hazard>()
			.Select(hazard => hazard.Clone())
			.ToList();

		foreach (var hazard in _removed)
			world.MutableNonUnits.Remove(hazard.Id);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var hazard in _removed)
			world.MutableNonUnits[hazard.Id] = hazard;
	}
}
