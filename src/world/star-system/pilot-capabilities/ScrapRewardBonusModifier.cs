using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.PilotCapabilities;

public sealed class ScrapRewardBonusModifier(int bonusPercent)
	: IActionEffectModifier<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public const int MinorBonusPercent = 5;
	public const int StandardBonusPercent = PilotSkillPercents.DronesYearnForMines.ScrapRewardBonusPercent;

	public IEffect<StarMap, ActorRuntime> Modify(
		IAction action,
		StarMap world,
		ActorRuntime runtime,
		IEffect<StarMap, ActorRuntime> effect)
	{
		if (bonusPercent == 0)
			return effect;

		return effect switch
		{
			ChangeResourceEffect resources => ModifyResources(resources),
			AwardEngagementLootEffect loot => ModifyLoot(loot),
			_ => effect,
		};
	}

	private ChangeResourceEffect ModifyResources(ChangeResourceEffect effect)
	{
		var modified = ModifyBundle(effect.Change);
		return ReferenceEquals(modified, effect.Change) ? effect : effect.WithChange(modified);
	}

	private AwardEngagementLootEffect ModifyLoot(AwardEngagementLootEffect effect)
	{
		var changed = false;
		var rolls = effect.Rolls
			.Select(roll =>
			{
				var awarded = ModifyBundle(roll.Awarded);
				changed |= !ReferenceEquals(awarded, roll.Awarded);
				return ReferenceEquals(awarded, roll.Awarded)
					? roll
					: roll with { Awarded = awarded };
			})
			.ToArray();
		if (!changed)
			return effect;

		var totals = new Dictionary<ResourceId, int>();
		foreach (var roll in rolls)
		{
			foreach (var (id, amount) in roll.Awarded)
				totals[id] = totals.GetValueOrDefault(id) + amount;
		}

		return effect.WithLoot(rolls, ResourceBundle.Create(totals));
	}

	private ResourceBundle ModifyBundle(ResourceBundle resources)
	{
		if (!resources.TryGet(ResourceId.ScrapAlloy, out var scrap) || scrap <= 0)
			return resources;

		return ResourceBundle.Create(resources.ToDictionary(
			entry => entry.Key,
			entry => entry.Key == ResourceId.ScrapAlloy
				? ScaleReward(entry.Value, bonusPercent)
				: entry.Value));
	}

	private static int ScaleReward(int amount, int bonusPercent) =>
		(int)decimal.Round(
			amount * (100m + bonusPercent) / 100m,
			0,
			MidpointRounding.AwayFromZero);
}
