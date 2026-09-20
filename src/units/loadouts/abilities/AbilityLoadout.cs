using GrimSpace.Units;

namespace GrimSpace.Units.Loadouts.Abilities;

public static class AbilityLoadout
{
	public static int PerTurnUsesForAbility(ShipInstance ship, EAbilityKind ability)
	{
		foreach (var installed in ship.Spec.InstalledAbilities)
		{
			if (installed.Spec.Kind != ability)
				continue;

			if (installed.Spec is IPerTurnAbility perTurn)
				return perTurn.UsesPerTurn;
		}

		return 0;
	}
}
