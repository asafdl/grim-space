using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
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
		HazardResolution.ApplyScheduledResolve(
			kind,
			cells,
			damage,
			momentumLoss,
			world.StateOf(actorId).Position,
			actorId,
			world.Units.Values.Select(unit => unit.State));
	}
}
