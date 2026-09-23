using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

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
			ShipSpec.Create(
				EType.Fighter,
				maxHullPoints: 2,
				ShipCatalog.DefaultFor(EType.Fighter).MaxShieldPoints,
				installed));
	}

	[Fact]
	public void SameKindOnDifferentFacets_IsAllowed()
	{
		var flak = new FlakSpec(UsesPerTurn: 1, Damage: 1, BurstRange: 2);
		var spec = ShipSpec.Create(
			EType.Fighter,
			maxHullPoints: 2,
			ShipCatalog.DefaultFor(EType.Fighter).MaxShieldPoints,
			[
				new InstalledAbility(flak, GrimSpace.Math.Grid.ESpatialOrientation.Port),
				new InstalledAbility(flak, GrimSpace.Math.Grid.ESpatialOrientation.Starboard),
			]);

		Assert.Equal(2, spec.InstalledAbilities.Count);
	}

	[Fact]
	public void EachFacetMount_YieldsIndependentRuntime()
	{
		var snapshot = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var flak = snapshot.Spec.InstalledAbilities
			.Where(ability => ability.Spec.Kind == EAbilityKind.Flak)
			.ToArray();

		Assert.Equal(2, flak.Length);
		Assert.All(flak, installed =>
			Assert.Equal(1, installed.Spec.CreateInitialRuntime().UsesRemaining));
	}

	[Fact]
	public void FighterCatalog_HasExpectedMounts()
	{
		var installed = ShipCatalog.DefaultInstalledAbilitiesFor(EType.Fighter);

		Assert.Equal(6, installed.Count);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.Flak);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.Railgun);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.TorpedoLauncher);
	}

	[Fact]
	public void PatrolFlak_HasIndependentPortAndStarboardMounts()
	{
		var installed = ShipCatalog.DefaultInstalledAbilitiesFor(EType.Patrol);

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
