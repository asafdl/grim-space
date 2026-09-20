using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.World.StarSystem.Dockyard;

internal static class AbilityUpgradeCatalog
{
	public static bool CanUpgrade(AbilitySpec spec) =>
		spec switch
		{
			FlakSpec or RailgunSpec => true,
			_ => false,
		};

	public static bool TryCreateUpgraded(AbilitySpec spec, out AbilitySpec upgraded)
	{
		upgraded = spec switch
		{
			FlakSpec flak => flak with
			{
				Damage = flak.Damage + 1,
				UpgradeTier = flak.UpgradeTier + 1,
			},
			RailgunSpec railgun => railgun with
			{
				Damage = railgun.Damage + 1,
				UpgradeTier = railgun.UpgradeTier + 1,
			},
			_ => null!,
		};

		return upgraded is not null;
	}
}
