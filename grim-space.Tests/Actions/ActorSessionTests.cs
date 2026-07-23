using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;

namespace GrimSpace.Tests.Actions;

public sealed class ActorSessionTests
{
	[Fact]
	public void NetYawWrapsRawYawQuarters()
	{
		var session = new ActorSession { RawYawQuarters = 5 };

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
		var session = new ActorSession { RawYawQuarters = rawQuarters };

		Assert.Equal(expectedNetYaw, session.NetYaw);
	}

	[Fact]
	public void IsMovePathStartedWhenPathFieldsAreSet()
	{
		var session = new ActorSession();

		Assert.False(session.IsMovePathStarted);

		session.UsedDirectionsMask = 1;

		Assert.True(session.IsMovePathStarted);
	}

	[Fact]
	public void ResetResetsAllFields()
	{
		var session = new ActorSession
		{
			RawYawQuarters = 2,
			MomentumPaid = 1,
			SpinBraked = true,
			SpinDiscount = true,
			PathForwardSteps = 1,
			UsedDirectionsMask = 1,
		};

		session.Reset();

		Assert.Equal(0, session.RawYawQuarters);
		Assert.Equal(0, session.MomentumPaid);
		Assert.False(session.SpinBraked);
		Assert.False(session.SpinDiscount);
		Assert.False(session.IsMovePathStarted);
	}
}
