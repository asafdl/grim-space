using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class AddHazardEffect(Hazard hazard) : IEffect<BattleWorld, ActorRuntime>
{
	private bool _added;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (world.NonUnits.ContainsKey(hazard.Id))
			throw new InvalidOperationException($"Non-unit '{hazard.Id}' already exists.");

		world.AddNonUnit(hazard);
		_added = true;

		return hazard switch
		{
			GoopHazard goop => [RecordGoopSpawned(goop)],
			_ => [],
		};
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_added)
			return;

		world.RemoveNonUnit(hazard.Id);
		_added = false;
	}

	private static Record<GoopSpawnedFacts> RecordGoopSpawned(GoopHazard goop) =>
		new(new GoopSpawnedFacts(
			goop.ActorId,
			goop.Id,
			goop.Center,
			goop.Cells));
}
