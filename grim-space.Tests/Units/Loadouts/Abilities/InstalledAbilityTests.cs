using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units.Loadouts.Abilities;

[BattleTestSuite]
public sealed class InstalledAbilityTests
{
	[Fact]
	public void DuplicateKindOnSameFacet_IsRejected()
	{
		var swarm = ScrapDroneSwarmSpec.Baseline;
		var installed = new[]
		{
			new InstalledAbility(swarm, GrimSpace.Math.Grid.ESpatialOrientation.Port),
			new InstalledAbility(swarm, GrimSpace.Math.Grid.ESpatialOrientation.Port),
		};

		Assert.Throws<ArgumentException>(() =>
			ShipLoadout.Create(
				FighterSpec.Instance,
				baseHullPoints: FighterSpec.Instance.DefaultBaseHullPoints,
				ShipCatalog.NewRunLoadoutFor(EType.Fighter).BaseShieldPoints,
				installed));
	}

	[Fact]
	public void SameKindOnDifferentFacets_IsAllowed()
	{
		var swarm = ScrapDroneSwarmSpec.Baseline;
		var loadout = ShipLoadout.Create(
			RepurposedMinerSpec.Instance,
			baseHullPoints: 2,
			ShipCatalog.NewRunLoadoutFor(EType.RepurposedMiner).BaseShieldPoints,
			[
				new InstalledAbility(swarm, GrimSpace.Math.Grid.ESpatialOrientation.Port),
				new InstalledAbility(swarm, GrimSpace.Math.Grid.ESpatialOrientation.Starboard),
			]);

		Assert.Equal(2, loadout.InstalledAbilities.Count);
	}

	[Fact]
	public void EachFacetMount_YieldsIndependentRuntime()
	{
		var snapshot = ShipInstance.FromCatalog("miner-a", EType.RepurposedMiner);
		var swarm = snapshot.Loadout.InstalledAbilities
			.Where(ability => ability.Spec.Kind == EAbilityKind.ScrapDroneSwarm)
			.ToArray();

		Assert.Equal(2, swarm.Length);
		Assert.All(swarm, installed =>
			Assert.Equal(1, installed.Spec.CreateInitialRuntime().UsesRemaining));
	}

	[Fact]
	public void FighterNewRun_HasStarterMountsAtBaseline()
	{
		var installed = ShipCatalog.NewRunLoadoutFor(EType.Fighter).InstalledAbilities;

		Assert.Equal(2, installed.Count);
		Assert.All(installed, ability =>
		{
			Assert.Equal(0, ability.DamageUpgradeTier);
			Assert.Equal(0, ability.RangeUpgradeTier);
		});
		Assert.Contains(
			installed,
			ability => ability.Mount == new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward));
		Assert.Contains(
			installed,
			ability => ability.Mount == new AbilityMount(EAbilityKind.VoidBombLauncher, ESpatialOrientation.Ventral));
	}

	[Fact]
	public void FighterFullCatalog_HasAllWeaponMounts()
	{
		var installed = ShipCatalog.FullFighterLoadout().InstalledAbilities;

		Assert.Equal(2, installed.Count);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.LightningCannon);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.VoidBombLauncher);
		Assert.DoesNotContain(installed, ability => ability.Spec.Kind == EAbilityKind.ScrapDroneSwarm);
		Assert.DoesNotContain(installed, ability => ability.Spec.Kind == EAbilityKind.GoopGun);
	}

	[Fact]
	public void RepurposedMinerScrapDroneSwarm_HasIndependentPortAndStarboardMounts()
	{
		var installed = ShipCatalog.NewRunLoadoutFor(EType.RepurposedMiner).InstalledAbilities;

		Assert.Equal(3, installed.Count);
		Assert.Contains(installed, ability => ability.Mount == new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port));
		Assert.Contains(installed, ability => ability.Mount == new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Starboard));
		Assert.Contains(installed, ability => ability.Mount == new AbilityMount(EAbilityKind.ProtectionDrone, ESpatialOrientation.Forward));
	}

	[Fact]
	public void UnitsProject_HasNoBattleReferences()
	{
		var unitsRoot = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "..",
			"src", "units"));
		var battleReferences = Directory
			.EnumerateFiles(unitsRoot, "*.cs", SearchOption.AllDirectories)
			.Where(path => File.ReadAllText(path).Contains("GrimSpace.Battle", StringComparison.Ordinal))
			.ToArray();

		Assert.Empty(battleReferences);
	}
}
