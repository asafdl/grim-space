using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class ResetCooldownOnKillReaction : IEffectReaction<BattleWorld, ActorRuntime>
{
	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> React(
		BattleEffectContext context,
		IEffect<BattleWorld, ActorRuntime> effect,
		IReadOnlyList<IRecord> records)
	{
		if (effect is not DamageUnitEffect
			|| records is not [Record<ImpactFacts> { Value: { IsLethal: true } impact }])
			return [];

		return KillCredit.Credited(context.World, impact)
			.SelectMany(state => state.Loadout.InstalledAbilities
				.Where(installed => installed.Spec is ICooldownAbility { ResetsCooldownOnKill: true }
					&& state.MountRuntimeFor(installed.Mount).CooldownRemaining > 0)
				.Select(installed => new ResetCooldownOnKillEffect(state.Id, installed.Mount)))
			.ToArray();
	}
}
