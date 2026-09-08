using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.Factions;

public sealed class FactionCatalogTests(DevStarMapFixture maps)
{
	[Fact]
	public void DevDefaultStarSystem_IsControlledByTheOptimality()
	{
		var map = maps.Fresh();

		Assert.Equal(EFaction.TheOptimality, map.ControllingFaction);
		Assert.Equal("The Optimality", FactionCatalog.DisplayName(map.ControllingFaction));
	}
}
