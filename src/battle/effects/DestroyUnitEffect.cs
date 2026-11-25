using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class DestroyUnitEffect(string unitId) : IEffect<BattleWorld, ActorRuntime>
{
	private int _hullBefore;

	public string UnitId => unitId;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var unit = world.StateOf(unitId);
		_hullBefore = unit.CurrentHullPoints;
		unit.CurrentHullPoints = 0;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(unitId).CurrentHullPoints = _hullBefore;
}
