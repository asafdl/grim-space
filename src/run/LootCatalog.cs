using GrimSpace.Battle.Objectives;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run;

public static class LootCatalog
{
	public static LootResult For(BattleOutcome outcome)
	{
		var rolls = new List<LootRoll>();
		foreach (var unit in outcome.TacticalUnitOutcomes)
		{
			if (unit.State != EBattleParticipantState.Destroyed)
				continue;
			if (!outcome.TryGetState(unit.OwnerParticipantId, out var ownerState)
				|| ownerState != EBattleParticipantState.Destroyed)
				continue;

			var awarded = Roll(unit.Kind);
			if (!awarded.IsEmpty)
				rolls.Add(new LootRoll(unit.TacticalUnitId, unit.Kind, awarded));
		}

		return new LootResult(rolls, SumRolls(rolls));
	}

	private static ResourceBundle Roll(EType kind) =>
		kind switch
		{
			EType.Patrol => ResourceBundle.Of(
				ResourceId.ScrapAlloy,
				Random.Shared.Next(50, 121)),
			_ => ResourceBundle.Empty,
		};

	private static ResourceBundle SumRolls(IReadOnlyList<LootRoll> rolls)
	{
		var totals = new Dictionary<ResourceId, int>();
		foreach (var roll in rolls)
		{
			foreach (var (id, amount) in roll.Awarded)
				totals[id] = totals.GetValueOrDefault(id) + amount;
		}

		return ResourceBundle.Create(totals);
	}
}

public sealed record LootResult(IReadOnlyList<LootRoll> Rolls, ResourceBundle Total);

public sealed record LootRoll(string TacticalUnitId, EType Kind, ResourceBundle Awarded);
