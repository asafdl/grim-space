using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.PilotCapabilities;

public sealed class ScrapRewardBonusRule : IEffectRule<StarMap, ActorRuntime>
{
	public const int MinorBonusPercent = 5;
	public const int StandardBonusPercent = PilotSkillPercents.DronesYearnForMines.ScrapRewardBonusPercent;

	public static int BonusPercent(IReadOnlyList<PilotCapabilityGrant> grants) =>
		grants.Sum(grant => grant.CapabilityId switch
		{
			EPilotCapabilityId.MinorScrapRewardBonus => MinorBonusPercent,
			EPilotCapabilityId.ScrapRewardBonus => StandardBonusPercent,
			_ => 0,
		});

	public bool Matches(EffectContext<StarMap, ActorRuntime> context, IEffect<StarMap, ActorRuntime> effect) =>
		effect is ChangeResourceEffect or AwardEngagementLootEffect
		&& context.Action.Tags.Contains(StarMapActionTags.Reward)
		&& context.Action.Tags.Contains(StarMapActionTags.Salvage)
		&& BonusPercent(context.Runtime.StrategicCapabilities) > 0;

	public IEffect<StarMap, ActorRuntime> Transform(
		EffectContext<StarMap, ActorRuntime> context,
		IEffect<StarMap, ActorRuntime> effect)
	{
		var bonusPercent = BonusPercent(context.Runtime.StrategicCapabilities);
		return effect switch
		{
			ChangeResourceEffect resources => ModifyResources(resources, bonusPercent),
			AwardEngagementLootEffect loot => ModifyLoot(loot, bonusPercent),
			_ => effect,
		};
	}

	private static ChangeResourceEffect ModifyResources(ChangeResourceEffect effect, int bonusPercent)
	{
		var modified = ModifyBundle(effect.Change, bonusPercent);
		return ReferenceEquals(modified, effect.Change) ? effect : effect.WithChange(modified);
	}

	private static AwardEngagementLootEffect ModifyLoot(AwardEngagementLootEffect effect, int bonusPercent)
	{
		var changed = false;
		var rolls = effect.Rolls
			.Select(roll =>
			{
				var awarded = ModifyBundle(roll.Awarded, bonusPercent);
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

	private static ResourceBundle ModifyBundle(ResourceBundle resources, int bonusPercent)
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
