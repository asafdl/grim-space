using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Dockyard;

[StarSystemTestSuite]
public sealed class DockyardHullRepairTests
{
	[Fact]
	public void TryApply_RestoresHullToMax()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		ship.HullPoints = 1;

		Assert.True(DockyardHullRepair.TryApply(ship, out var after));
		Assert.Equal(ship.Spec.MaxHullPoints, after.HullPoints);
	}

	[Fact]
	public void TryQuote_ChargesFixedCreditsAndScrap()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		ship.HullPoints = 1;

		Assert.True(DockyardHullRepair.TryQuote(ship, out var cost));
		Assert.True(cost.TryGet(ResourceId.Credits, out var credits));
		Assert.True(cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(DockyardHullRepair.CreditCost, credits);
		Assert.Equal(DockyardHullRepair.ScrapCost, scrap);
	}

	[Fact]
	public void TryQuote_FailsWhenHullIsFull()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		Assert.False(DockyardHullRepair.TryQuote(ship, out _));
	}
}
