using GrimSpace.Battle.Ids;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class ClearTurnHazardsEffect : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices)
	{
		var turnScoped = slices.Board.NonUnits.Values
			.Where(nonUnit => nonUnit.OwnerId != EntityIds.Board)
			.Select(nonUnit => nonUnit.Id)
			.ToList();

		foreach (var id in turnScoped)
			slices.Board.MutableNonUnits.Remove(id);
	}
}
