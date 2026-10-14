using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.PilotSkills;

public static class StrategicPilotSkillModifiers
{
	public const int ScrapBonusPercentPerPilot = 15;
	public const int UpgradeDiscountPercentPerPilot = 10;

	public static ResourceBundle ApplySalvageBonus(
		StarMap map,
		ResourceBundle resources,
		IReadOnlyList<PilotSkillGrant>? strategicGrants = null)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(resources);
		var stacks = Count(strategicGrants, EPilotSkillId.DronesYearnForMines);
		if (stacks == 0 || resources.IsEmpty)
			return resources;

		return Transform(resources, (id, amount) =>
			id == ResourceId.ScrapAlloy
				? ScaleReward(amount, ScrapBonusPercentPerPilot * stacks)
				: amount);
	}

	public static LootResult ApplySalvageBonus(
		StarMap map,
		LootResult loot,
		IReadOnlyList<PilotSkillGrant>? strategicGrants = null)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(loot);
		if (loot.Rolls.Count == 0)
			return loot;

		var rolls = loot.Rolls
			.Select(roll => roll with { Awarded = ApplySalvageBonus(map, roll.Awarded, strategicGrants) })
			.ToArray();
		var totals = new Dictionary<ResourceId, int>();
		foreach (var roll in rolls)
		{
			foreach (var (id, amount) in roll.Awarded)
				totals[id] = totals.GetValueOrDefault(id) + amount;
		}

		return new LootResult(rolls, ResourceBundle.Create(totals));
	}

	public static ResourceBundle ApplyUpgradeDiscount(
		StarMap map,
		ResourceBundle cost,
		IReadOnlyList<PilotSkillGrant>? strategicGrants = null)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(cost);
		var stacks = Count(strategicGrants, EPilotSkillId.IKnowABot);
		if (stacks == 0 || cost.IsEmpty)
			return cost;

		return Transform(cost, (id, amount) =>
			id is ResourceId.Credits or ResourceId.ScrapAlloy
				? ScaleCost(amount, UpgradeDiscountPercentPerPilot * stacks)
				: amount);
	}

	public static int Count(IReadOnlyList<PilotSkillGrant>? grants, EPilotSkillId skillId) =>
		grants?.Count(grant => grant.SkillId == skillId) ?? 0;

	private static int ScaleReward(int amount, int bonusPercent) =>
		(int)decimal.Round(
			amount * (100m + bonusPercent) / 100m,
			0,
			MidpointRounding.AwayFromZero);

	private static int ScaleCost(int amount, int discountPercent)
	{
		var remainingPercent = System.Math.Max(0, 100 - discountPercent);
		return (int)decimal.Ceiling(amount * remainingPercent / 100m);
	}

	private static ResourceBundle Transform(
		ResourceBundle resources,
		Func<ResourceId, int, int> transform) =>
		ResourceBundle.Create(resources.ToDictionary(
			entry => entry.Key,
			entry => transform(entry.Key, entry.Value)));
}
