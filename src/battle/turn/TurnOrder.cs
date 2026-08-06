using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Turn;

public static class TurnOrder
{
	public static IEnumerable<Unit> Living(IEnumerable<Unit> units) =>
		units
			.Where(unit => unit.State.IsAlive)
			.OrderBy(Rank)
			.ThenBy(unit => unit.State.Id, StringComparer.Ordinal);

	private static int Rank(Unit unit) =>
		unit.State.Type == EType.Torpedo ? 2
		: unit.Controller == EController.Player ? 0
		: 1;
}
