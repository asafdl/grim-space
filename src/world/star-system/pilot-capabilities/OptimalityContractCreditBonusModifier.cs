using GrimSpace.Core.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.PilotCapabilities;

public sealed class OptimalityContractCreditBonusModifier(int stackCount)
	: IActionEffectModifier<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public const int CreditBonusPercentPerStack = 5;

	public IEffect<StarMap, ActorRuntime> Modify(
		IAction action,
		StarMap world,
		ActorRuntime runtime,
		IEffect<StarMap, ActorRuntime> effect)
	{
		if (stackCount == 0
			|| effect is not ChangeResourceEffect resources
			|| !resources.Change.TryGet(ResourceId.Credits, out var credits)
			|| credits <= 0)
			return effect;

		var bonusPercent = CreditBonusPercentPerStack * stackCount;
		var changed = ResourceBundle.Create(resources.Change.ToDictionary(
			entry => entry.Key,
			entry => entry.Key == ResourceId.Credits
				? ScaleReward(entry.Value, bonusPercent)
				: entry.Value));
		return resources.WithChange(changed);
	}

	private static int ScaleReward(int amount, int bonusPercent) =>
		(int)decimal.Round(
			amount * (100m + bonusPercent) / 100m,
			0,
			MidpointRounding.AwayFromZero);
}
