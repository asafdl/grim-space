using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Turn;

namespace GrimSpace.Tests.Actions;

public sealed class TurnPhaseContextTests
{
	[Fact]
	public void NetYawWrapsRawYawQuarters()
	{
		var context = new TurnPhaseContext { RawYawQuarters = 5 };

		Assert.Equal(1, context.NetYaw);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(4, 0)]
	[InlineData(-1, 3)]
	[InlineData(5, 1)]
	public void NetYawWrapsToModRange(int rawQuarters, int expectedNetYaw)
	{
		var context = new TurnPhaseContext { RawYawQuarters = rawQuarters };

		Assert.Equal(expectedNetYaw, context.NetYaw);
	}

	[Fact]
	public void IsMovePathStartedWhenPathFieldsAreSet()
	{
		var context = new TurnPhaseContext();

		Assert.False(context.IsMovePathStarted);

		context.UsedDirectionsMask = 1;

		Assert.True(context.IsMovePathStarted);
	}

	[Fact]
	public void ResetResetsAllFields()
	{
		var context = new TurnPhaseContext
		{
			RawYawQuarters = 2,
			MomentumPaid = 1,
			SpinBraked = true,
			SpinDiscount = true,
			PathForwardSteps = 1,
			UsedDirectionsMask = 1,
			FlakUsedThisTurn = true,
		};

		context.Reset();

		Assert.Equal(0, context.RawYawQuarters);
		Assert.Equal(0, context.MomentumPaid);
		Assert.False(context.SpinBraked);
		Assert.False(context.SpinDiscount);
		Assert.False(context.IsMovePathStarted);
		Assert.False(context.FlakUsedThisTurn);
	}
}
