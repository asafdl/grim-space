using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class ClearTurnHazardsEffect : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices)
	{
		var turnScoped = slices.Board.NonUnits.Values
			.Where(nonUnit => nonUnit.OwnerId != EntityIds.World)
			.Select(nonUnit => nonUnit.Id)
			.ToList();

		foreach (var id in turnScoped)
			slices.Board.MutableNonUnits.Remove(id);
	}
}
