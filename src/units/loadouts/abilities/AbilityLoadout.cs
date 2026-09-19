using GrimSpace.Units;

namespace GrimSpace.Units.Loadouts.Abilities;

public static class AbilityLoadout
{
	public static IReadOnlyDictionary<AbilityMount, int> UsesPerMount(ShipSnapshot snapshot)
	{
		var uses = new Dictionary<AbilityMount, int>();
		foreach (var mount in snapshot.Configuration.AbilityMounts)
		{
			_ = AbilityDefinition.For(mount.Ability);
			uses[mount] = mount.Definition.UsesPerTurn;
		}

		return uses;
	}

	public static int UsesPerTurnForAbility(ShipSnapshot snapshot, EAbilityKind ability)
	{
		if (!snapshot.Configuration.AbilityMounts.Any(mount => mount.Ability == ability))
			return 0;

		return AbilityDefinition.For(ability).UsesPerTurn;
	}
}
