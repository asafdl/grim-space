using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class ResolveHazardEffect(
	EHazardKind kind,
	HashSet<Coord> cells,
	int damage,
	int momentumLoss) : IEffect<BattleWorld, ActorRuntime>
{
	private Dictionary<string, UnitCombatSnapshot> _snapshots = [];

	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_snapshots = new Dictionary<string, UnitCombatSnapshot>();
		var units = UnitRegistry.For(world);
		foreach (var unit in units.All)
		{
			if (!unit.State.IsAlive || !cells.Contains(unit.State.Position))
				continue;

			_snapshots[unit.State.Id] = UnitCombatSnapshot.Capture(unit.State);
		}

		HazardResolution.ApplyScheduledResolve(
			kind,
			cells,
			damage,
			momentumLoss,
			world.StateOf(actorId).Position,
			actorId,
			units.All.Select(unit => unit.State));
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (unitId, snapshot) in _snapshots)
			snapshot.Restore(world.StateOf(unitId));
	}
}
