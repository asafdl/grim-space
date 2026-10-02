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
		var swarm = new ScrapDroneSwarmSpec(UsesPerTurn: 1, Damage: 1, BurstRange: 2);
		var installed = new[]
		{
			new InstalledAbility(swarm, GrimSpace.Math.Grid.ESpatialOrientation.Port),
			new InstalledAbility(swarm, GrimSpace.Math.Grid.ESpatialOrientation.Port),
		};

		Assert.Throws<ArgumentException>(() =>
			ShipLoadout.Create(
				FighterSpec.Instance,
				maxHullPoints: 2,
				ShipCatalog.NewRunLoadoutFor(EType.Fighter).MaxShieldPoints,
				installed));
	}

	[Fact]
	public void SameKindOnDifferentFacets_IsAllowed()
	{
		var swarm = new ScrapDroneSwarmSpec(UsesPerTurn: 1, Damage: 1, BurstRange: 2);
		var loadout = ShipLoadout.Create(
			FighterSpec.Instance,
			maxHullPoints: 2,
			ShipCatalog.NewRunLoadoutFor(EType.Fighter).MaxShieldPoints,
			[
				new InstalledAbility(swarm, GrimSpace.Math.Grid.ESpatialOrientation.Port),
				new InstalledAbility(swarm, GrimSpace.Math.Grid.ESpatialOrientation.Starboard),
			]);

		Assert.Equal(2, loadout.InstalledAbilities.Count);
	}

	[Fact]
	public void EachFacetMount_YieldsIndependentRuntime()
	{
		var snapshot = ShipInstance.FromSpec(
			"fighter-a",
			FighterSpec.Instance,
			ShipCatalog.FullFighterLoadout());
		var swarm = snapshot.Loadout.InstalledAbilities
			.Where(ability => ability.Spec.Kind == EAbilityKind.ScrapDroneSwarm)
			.ToArray();

		Assert.Equal(2, swarm.Length);
		Assert.All(swarm, installed =>
			Assert.Equal(1, installed.Spec.CreateInitialRuntime().UsesRemaining));
	}

	[Fact]
	public void FighterNewRun_HasStarterMounts()
	{
		var installed = ShipCatalog.NewRunLoadoutFor(EType.Fighter).InstalledAbilities;

		Assert.Equal(2, installed.Count);
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

		Assert.Equal(6, installed.Count);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.ScrapDroneSwarm);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.LightningCannon);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.VoidBombLauncher);
	}

	[Fact]
	public void RepurposedMinerScrapDroneSwarm_HasIndependentPortAndStarboardMounts()
	{
		var installed = ShipCatalog.NewRunLoadoutFor(EType.RepurposedMiner).InstalledAbilities;

		Assert.Equal(2, installed.Count);
		Assert.Contains(installed, ability => ability.Mount == new AbilityMount(EAbilityKind.ScrapDroneSwarm, GrimSpace.Math.Grid.ESpatialOrientation.Port));
		Assert.Contains(installed, ability => ability.Mount == new AbilityMount(EAbilityKind.ScrapDroneSwarm, GrimSpace.Math.Grid.ESpatialOrientation.Starboard));
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
