using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ResolveHazardEffect(string hazardId) : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		if (!world.NonUnits.TryGetValue(hazardId, out var nonUnit) || nonUnit is not Hazard hazard)
			return;

		foreach (var unit in world.Units.Values)
		{
			if (!unit.State.IsAlive || !hazard.Cells.Contains(unit.State.Position))
				continue;

			HazardResolution.ApplyToUnitAt(hazard, unit.State);
		}
	}
}
