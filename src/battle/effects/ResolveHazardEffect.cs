using GrimSpace.Battle.Board;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class ResolveHazardEffect(string hazardId) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices)
	{
		if (!slices.Board.NonUnits.TryGetValue(hazardId, out var nonUnit) || nonUnit is not Hazard hazard)
			return;

		foreach (var unit in slices.Board.Units.Values)
		{
			if (!unit.State.IsAlive || !hazard.Cells.Contains(unit.State.Position))
				continue;

			HazardResolution.ApplyToUnitAt(hazard, unit.State);
		}
	}
}
