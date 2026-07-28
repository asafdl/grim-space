using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class ResolveHazardEffect(
	EHazardKind kind,
	HashSet<Coord> cells,
	int damage,
	int momentumLoss) : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var center = cells.Count > 0 ? cells.First() : Coord.Zero;
		var hazard = new Hazard
		{
			Id = string.Empty,
			ActorId = actorId,
			Center = center,
			Frame = BodyFrame.WorldAligned(center),
			Cells = cells,
			Passable = true,
			Damage = damage,
			MomentumLoss = momentumLoss,
			Kind = kind,
		};

		foreach (var unit in world.Units.Values)
		{
			if (!unit.State.IsAlive || !cells.Contains(unit.State.Position))
				continue;

			HazardResolution.ApplyToUnitAt(hazard, unit.State);
		}
	}
}
