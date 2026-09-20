using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Units.Loadouts.Abilities;

public sealed class AbilityLoadoutTests
{
	[Fact]
	public void MultiFacetMount_YieldsOneRuntimeSlot()
	{
		var snapshot = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var installed = snapshot.Spec.InstalledAbilities.Single(ability => ability.Spec.Kind == EAbilityKind.Flak);

		Assert.Equal(1, installed.ForState().UsesRemaining);
	}

	[Fact]
	public void FighterCatalog_HasExpectedMounts()
	{
		var installed = ShipCatalog.DefaultInstalledAbilitiesFor(EType.Fighter);

		Assert.Equal(3, installed.Count);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.Flak);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.Railgun);
		Assert.Contains(installed, ability => ability.Spec.Kind == EAbilityKind.TorpedoLauncher);
	}

	[Fact]
	public void PatrolFlak_IsSingleMountWithPortAndStarboardFacets()
	{
		var installed = ShipCatalog.DefaultInstalledAbilitiesFor(EType.Patrol).Single();

		Assert.Equal(EAbilityKind.Flak, installed.Spec.Kind);
		Assert.Equal(2, installed.Facets.Count);
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
