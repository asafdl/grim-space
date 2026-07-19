using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions.Battle;

namespace GrimSpace.Tests.Actions;

public sealed class TurnStateTests
{
	[Fact]
	public void AddYawQuartersAccumulatesRawValue()
	{
		var state = new TurnState();

		state.AddYawQuarters(1);
		state.AddYawQuarters(2);

		Assert.Equal(3, state.RawYawQuarters);
		Assert.Equal(3, state.NetYaw);
	}

	[Fact]
	public void TryConsumeSpinDiscountRemovesDiscountWhenAvailable()
	{
		var state = new TurnState();
		state.MarkBrakedFromRetro();

		Assert.True(state.TryConsumeSpinDiscount());
		Assert.False(state.HasSpinDiscount);
		Assert.True(state.SpinBraked);
	}

	[Fact]
	public void TryConsumeSpinDiscountFailsWhenNoDiscount()
	{
		var state = new TurnState();

		Assert.False(state.TryConsumeSpinDiscount());
	}

	[Fact]
	public void IsMovePathStartedAfterRecordingMoveStep()
	{
		var state = new TurnState();

		Assert.False(state.IsMovePathStarted);

		state.RecordMoveStep(EStepDirection.Forward, directionBit: 1);

		Assert.True(state.IsMovePathStarted);
	}

	[Fact]
	public void ClearResetsAllTurnState()
	{
		var state = new TurnState();
		state.AddYawQuarters(2);
		state.AddMomentumPaid(1);
		state.MarkBrakedFromRetro();
		state.RecordMoveStep(EStepDirection.Forward, directionBit: 1);
		state.Clear();

		Assert.Equal(0, state.RawYawQuarters);
		Assert.Equal(0, state.MomentumPaidThisTurn);
		Assert.False(state.SpinBraked);
		Assert.False(state.HasSpinDiscount);
		Assert.False(state.IsMovePathStarted);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(4, 0)]
	[InlineData(-1, 3)]
	[InlineData(5, 1)]
	public void NetYawWrapsToModRange(int rawQuarters, int expectedNetYaw)
	{
		var state = new TurnState();
		state.AddYawQuarters(rawQuarters);

		Assert.Equal(expectedNetYaw, state.NetYaw);
	}
}
