using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.PilotCapabilities;

public sealed class OptimalityContractCreditBonusRule : IEffectRule<StarMap, ActorRuntime>
{
	public const int CreditBonusPercentPerStack = 5;

	public bool Matches(EffectContext<StarMap, ActorRuntime> context, IEffect<StarMap, ActorRuntime> effect) =>
		effect is ChangeResourceEffect resources
		&& resources.Change.TryGet(ResourceId.Credits, out var credits)
		&& credits > 0
		&& context.Action.Tags.Contains(StarMapActionTags.Reward)
		&& context.Action.Tags.Contains(StarMapActionTags.Contract)
		&& context.Action.Tags.Contains(StarMapActionTags.TheOptimality)
		&& Stacks(context.Runtime) > 0;

	public IEffect<StarMap, ActorRuntime> Transform(
		EffectContext<StarMap, ActorRuntime> context,
		IEffect<StarMap, ActorRuntime> effect)
	{
		var resources = (ChangeResourceEffect)effect;
		var bonusPercent = CreditBonusPercentPerStack * Stacks(context.Runtime);
		return resources.WithChange(ResourceBundle.Create(resources.Change.ToDictionary(
			entry => entry.Key,
			entry => entry.Key == ResourceId.Credits
				? ScaleReward(entry.Value, bonusPercent)
				: entry.Value)));
	}

	private static int Stacks(ActorRuntime runtime) =>
		runtime.StrategicCapabilities.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.OptimalityContractCreditBonus);

	private static int ScaleReward(int amount, int bonusPercent) =>
		(int)decimal.Round(
			amount * (100m + bonusPercent) / 100m,
			0,
			MidpointRounding.AwayFromZero);
}
