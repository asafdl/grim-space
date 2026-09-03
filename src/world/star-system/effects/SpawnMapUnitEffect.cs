using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SpawnMapUnitEffect : IEffect<StarMap, ActorRuntime>
{
	private readonly Unit _unit;

	public SpawnMapUnitEffect(Unit unit) => _unit = unit;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (world.UnitRegistry.Contains(_unit.State.Id))
		{
			GameLog.Log($"[star-map] spawn skipped: unit '{_unit.State.Id}' already exists.");
			return [];
		}

		world.UnitRegistry.Add(_unit);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		world.UnitRegistry.Remove(_unit.State.Id);
}
