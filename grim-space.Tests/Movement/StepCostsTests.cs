using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Tests.Movement;

public sealed class StepCostsTests
{
	[Theory]
	[InlineData(0, 0, 1)]
	[InlineData(1, 0, 0)]
	[InlineData(1, 1, 1)]
	[InlineData(2, 1, 0)]
	[InlineData(3, 2, 0)]
	public void FirstForwardStepsUpToMomentumLevelAreFree(
		int momentum,
		int forwardStepsAlreadyTaken,
		int expectedCost)
	{
		var cost = StepCosts.GetMoveStepApCost(
			EStepDirection.Forward,
			new MoveStepContext(forwardStepsAlreadyTaken, momentum));

		Assert.Equal(MovementExpectations.ForwardStepApCost(forwardStepsAlreadyTaken, momentum), expectedCost);
		Assert.Equal(expectedCost, cost);
	}

	[Fact]
	public void LateralDriftCostsIncreaseWithMomentum()
	{
		var previous = 0;

		for (var momentum = 0; momentum <= MovementExpectations.MaxMomentum; momentum++)
		{
			var cost = StepCosts.GetMoveStepApCost(
				EStepDirection.Starboard,
				new MoveStepContext(0, momentum));

			Assert.True(cost > 0);
			Assert.True(momentum == 0 || cost > previous);
			previous = cost;
		}
	}

	[Fact]
	public void BrakingCostsMoreThanForwardThrustAndRisesWithMomentum()
	{
		var previous = 0;

		for (var momentum = 0; momentum <= MovementExpectations.MaxMomentum; momentum++)
		{
			var retro = StepCosts.GetMoveStepApCost(
				EStepDirection.Retro,
				new MoveStepContext(0, momentum));
			var forward = StepCosts.GetMoveStepApCost(
				EStepDirection.Forward,
				new MoveStepContext(momentum, momentum));

			Assert.True(retro > forward);
			Assert.True(momentum == 0 || retro > previous);
			previous = retro;
		}
	}
}
