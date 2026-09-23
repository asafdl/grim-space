using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Dockyard;

[StarSystemTestSuite]
public sealed class DockyardShieldRechargeTests
{
	[Fact]
	public void TryApply_RestoresAllFacesToMax()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		ship.ShieldPoints[ESpatialOrientation.Port] = 1;

		Assert.True(DockyardShieldRecharge.TryApply(ship, out var after));
		Assert.Equal(ship.Spec.MaxShieldPoints[ESpatialOrientation.Forward], after.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(ship.Spec.MaxShieldPoints[ESpatialOrientation.Port], after.ShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryQuote_ChargesTenCreditsPerMissingPoint()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		var max = DockyardShieldRecharge.TotalMax(ship);
		ship.ShieldPoints.Fill(0);

		Assert.True(DockyardShieldRecharge.TryQuote(ship, out var cost));
		Assert.True(cost.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(max * DockyardShieldRecharge.CreditsPerPoint, credits);
	}

	[Fact]
	public void TryApplyFace_RestoresOnlyRequestedFace()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		ship.ShieldPoints[ESpatialOrientation.Port] = 0;

		Assert.True(DockyardShieldRecharge.TryApplyFace(ship, ESpatialOrientation.Forward, out var after));
		Assert.Equal(ship.Spec.MaxShieldPoints[ESpatialOrientation.Forward], after.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(0, after.ShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryQuote_FailsWhenAlreadyFull()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		Assert.False(DockyardShieldRecharge.TryQuote(ship, out _));
	}
}
