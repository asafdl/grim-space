using GrimSpace.Battle.Objectives;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

public static class LootCatalog
{
	//TODO: need to find a better system, battle outcome is just the outcome of battle, has no concept of friend foe
	public static LootResult For(BattleOutcome outcome)
	{
		var rolls = new List<LootRoll>();
		foreach (var handoff in outcome.StateHandoffs)
		{
			if (handoff.HullPoints > 0)
				continue;

			var awarded = Roll(handoff.Chassis);
			if (!awarded.IsEmpty)
				rolls.Add(new LootRoll(handoff.Id, handoff.Chassis, awarded));
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
