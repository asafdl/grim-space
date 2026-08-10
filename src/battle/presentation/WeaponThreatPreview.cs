using GrimSpace.Battle.Presentation.Domains.Turn;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

/// <summary>Units (non-self) currently sitting in a weapon aim volume.</summary>
internal static class WeaponThreatPreview
{
	public static HashSet<string> UnitIdsInCells(
		BattleOrchestrator battle,
		IReadOnlySet<Coord> cells)
	{
		if (cells.Count == 0)
			return [];

		return TurnUi.GetPreviewWorld(battle)
			.UnitsInCells(battle.PlayerId, cells)
			.Where(static entry => entry.Relation != EUnitRelation.Self)
			.Select(static entry => entry.Unit.State.Id)
			.ToHashSet();
	}
}
