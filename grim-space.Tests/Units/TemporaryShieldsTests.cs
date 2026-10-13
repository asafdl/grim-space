using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

[BattleTestSuite]
public sealed class TemporaryShieldsTests
{
	[Fact]
	public void OverlappingGrants_StackAndExpireIndependently()
	{
		var unit = State.FromShipInstance(ShipInstance.FromCatalog("test", EType.Fighter), Coord.Zero);
		TemporaryShields.GrantPerFace(unit, "a", 1);
		TemporaryShields.GrantPerFace(unit, "b", 1);

		Assert.Equal(2, TemporaryShields.TotalOnFace(unit, ESpatialOrientation.Forward));

		Assert.Equal(1, Defense.AbsorbWithShields(unit, ESpatialOrientation.Forward, 1));
		Assert.Equal(1, TemporaryShields.TotalOnFace(unit, ESpatialOrientation.Forward));

		Assert.True(TemporaryShields.TryRevokeGrant(unit, "a", out _));
		Assert.Equal(1, TemporaryShields.TotalOnFace(unit, ESpatialOrientation.Forward));

		Assert.True(TemporaryShields.TryRevokeGrant(unit, "b", out _));
		Assert.Equal(0, TemporaryShields.TotalOnFace(unit, ESpatialOrientation.Forward));
	}

	[Fact]
	public void RevokeDepletedGrant_IsNoOp()
	{
		var unit = State.FromShipInstance(ShipInstance.FromCatalog("test", EType.Fighter), Coord.Zero);
		TemporaryShields.GrantPerFace(unit, "spent", 1);
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			Defense.AbsorbWithShields(unit, face, TemporaryShields.TotalOnFace(unit, face));

		Assert.False(TemporaryShields.TryRevokeGrant(unit, "spent", out _));
	}
}
