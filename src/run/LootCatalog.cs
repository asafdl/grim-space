using GrimSpace.Battle.Objectives;
using GrimSpace.World.StarSystem.Resources;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Run;

public static class LootCatalog
{
	//TODO: need to find a better system, battle outcome is just the outcome of battle, has no concept of friend foe
	public static LootResult For(BattleOutcome outcome)
	{
		var rolls = new List<LootRoll>();
		foreach (var handoff in outcome.StateHandoffs)
		{
			if (handoff.HP > 0)
				continue;

			var awarded = Roll(handoff.Kind);
			if (!awarded.IsEmpty)
				rolls.Add(new LootRoll(handoff.Id, handoff.Kind, awarded));
		}

		return new LootResult(rolls, SumRolls(rolls));
	}

	private static ResourceBundle Roll(BattleUnitType kind) =>
		kind switch
		{
			BattleUnitType.Patrol => ResourceBundle.Of(
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

public sealed record LootRoll(string TacticalUnitId, BattleUnitType Kind, ResourceBundle Awarded);
