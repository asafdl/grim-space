using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RemoveHazardEffect(string hazardId, string ownerActorId) : IEffect<BattleWorld, ActorRuntime>
{
	private Hazard? _removed;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!world.NonUnits.TryGetValue(hazardId, out var nonUnit)
			|| nonUnit is not Hazard hazard
			|| !string.Equals(hazard.ActorId, ownerActorId, StringComparison.Ordinal))
			return [];

		_removed = hazard;
		world.RemoveNonUnit(hazardId);

		return hazard switch
		{
			GoopHazard => [new Record<GoopDissipatedFacts>(new GoopDissipatedFacts(ownerActorId, hazardId))],
			_ => [],
		};
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_removed is null)
			return;

		world.AddNonUnit(_removed);
		_removed = null;
	}
}
