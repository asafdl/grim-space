using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Units.Loadouts.Abilities;

[BattleTestSuite]
public sealed class GoopGunSpecTests
{
	[Fact]
	public void GetArea_ReturnsNineCellsCenteredAtRange()
	{
		var spec = CatalogExpectations.DefaultGoopGunSpec();
		var origin = new Coord(5, 5, 5);
		var fore = Coord.Forward;
		var dorsal = Coord.Up;
		var cells = spec.GetArea(origin, fore, fore, dorsal);

		Assert.Equal(9, cells.Count);
		Assert.Contains(origin + fore * spec.Range, cells);
		Assert.DoesNotContain(origin, cells);
	}

	[Fact]
	public void RoundTripsThroughSaveDto()
	{
		var ship = BattleSpawnTestKit.FighterWithGoopGun("goop-test");
		var restored = SaveDtoMapper.RestoreShip(SaveDtoMapper.CaptureShip(ship));
		var spec = (GoopGunSpec)restored.Loadout.InstalledAbilities
			.Single(ability => ability.Spec.Kind == EAbilityKind.GoopGun).Spec;

		Assert.Equal(CatalogExpectations.DefaultGoopGunSpec(), spec);
	}
}
