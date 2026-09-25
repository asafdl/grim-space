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
		var flak = new FlakSpec(UsesPerTurn: 1, Damage: 1, BurstRange: 2);
		var installed = new[]
		{
			new InstalledAbility(flak, GrimSpace.Math.Grid.ESpatialOrientation.Port),
			new InstalledAbility(flak, GrimSpace.Math.Grid.ESpatialOrientation.Port),
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
		var flak = new FlakSpec(UsesPerTurn: 1, Damage: 1, BurstRange: 2);
		var loadout = ShipLoadout.Create(
			FighterSpec.Instance,
			maxHullPoints: 2,
			ShipCatalog.NewRunLoadoutFor(EType.Fighter).MaxShieldPoints,
			[
				new InstalledAbility(flak, GrimSpace.Math.Grid.ESpatialOrientation.Port),
				new InstalledAbility(flak, GrimSpace.Math.Grid.ESpatialOrientation.Starboard),
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
		var flak = snapshot.Loadout.InstalledAbilities
			.Where(ability => ability.Spec.Kind == EAbilityKind.Flak)
			.ToArray();

		Assert.Equal(2, flak.Length);
		Assert.All(flak, installed =>
			Assert.Equal(1, installed.Spec.CreateInitialRuntime().UsesRemaining));
	}

	[Fact]
	public void FighterNewRun_HasStarterMounts()
	{
		var installed = ShipCatalog.NewRunLoadoutFor(EType.Fighter).InstalledAbilities;

		Assert.Equal(2, installed.Count);
		Assert.Contains(
			installed,
			ability => ability.Mount == new AbilityMount(EAbilityKind.Railgun, ESpatialOrientation.Forward));
		Assert.Contains(
			installed,
			ability => ability.Mount == new AbilityMount(EAbilityKind.TorpedoLauncher, ESpatialOrientation.Ventral));
	}

	[Fact]
	public void FighterFullCatalog_HasAllWeaponMounts()
	{
		var installed = ShipCatalog.FullFighterLoadout().InstalledAbilities;

		Assert.Equal(6, installed.Count);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.Flak);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.Railgun);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.TorpedoLauncher);
	}

	[Fact]
	public void PatrolFlak_HasIndependentPortAndStarboardMounts()
	{
		var installed = ShipCatalog.NewRunLoadoutFor(EType.Patrol).InstalledAbilities;

		Assert.Equal(2, installed.Count);
		Assert.Contains(installed, ability => ability.Mount == new AbilityMount(EAbilityKind.Flak, GrimSpace.Math.Grid.ESpatialOrientation.Port));
		Assert.Contains(installed, ability => ability.Mount == new AbilityMount(EAbilityKind.Flak, GrimSpace.Math.Grid.ESpatialOrientation.Starboard));
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
