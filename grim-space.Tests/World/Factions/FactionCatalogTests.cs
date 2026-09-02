using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;

namespace GrimSpace.Tests.World.Factions;

public sealed class FactionCatalogTests
{
	[Fact]
	public void DevDefaultStarSystem_IsControlledByTheOptimality()
	{
		var map = StarMap.CreateDevDefault();

		Assert.Equal(EFaction.TheOptimality, map.ControllingFaction);
		Assert.Equal("The Optimality", FactionCatalog.DisplayName(map.ControllingFaction));
	}
}
