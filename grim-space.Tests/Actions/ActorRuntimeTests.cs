using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class ActorRuntimeTests
{
	[Fact]
	public void NetYawWrapsRawYawQuarters()
	{
		var session = new ActorRuntime { RawYawQuarters = 5 };

		Assert.Equal(1, session.NetYaw);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(4, 0)]
	[InlineData(-1, 3)]
	[InlineData(5, 1)]
	public void NetYawWrapsToModRange(int rawQuarters, int expectedNetYaw)
	{
		var session = new ActorRuntime { RawYawQuarters = rawQuarters };

		Assert.Equal(expectedNetYaw, session.NetYaw);
	}

	[Fact]
	public void ActivePathStartsNull()
	{
		var session = new ActorRuntime();

		Assert.Null(session.ActivePath);
	}

	[Fact]
	public void ResetClearsActivePath()
	{
		var session = new ActorRuntime
		{
			RawYawQuarters = 2,
			MomentumPaid = 1,
			SpinBraked = true,
			SpinDiscount = true,
			ActivePath = MovePathSession.Begin(
				"player",
				Coord.Zero,
				BodyFrame.WorldAligned(Coord.Zero),
				0,
				Stats.ForType(EType.Fighter).MinPathApCost),
		};

		session.Reset();

		Assert.Equal(0, session.RawYawQuarters);
		Assert.Equal(0, session.MomentumPaid);
		Assert.False(session.SpinBraked);
		Assert.False(session.SpinDiscount);
		Assert.Null(session.ActivePath);
	}
}
