using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Movement;

public static class Orientation
{
	public static GridBasis Roll(GridBasis basis, ERollDirection direction) =>
		direction switch
		{
			ERollDirection.Clockwise => new GridBasis(
				basis.Forward,
				-basis.Right,
				basis.Up),
			ERollDirection.CounterClockwise => new GridBasis(
				basis.Forward,
				basis.Right,
				-basis.Up),
			_ => throw new ArgumentOutOfRangeException(nameof(direction)),
		};

	public static GridBasis HeadingTurn(GridBasis basis, EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.YawRight => new GridBasis(
				basis.Right,
				basis.Up,
				-basis.Forward),
			EHeadingTurn.YawLeft => new GridBasis(
				-basis.Right,
				basis.Up,
				basis.Forward),
			EHeadingTurn.PitchUp => new GridBasis(
				basis.Up,
				-basis.Forward,
				basis.Right),
			EHeadingTurn.PitchDown => new GridBasis(
				-basis.Up,
				basis.Forward,
				basis.Right),
			_ => throw new ArgumentOutOfRangeException(nameof(turn)),
		};

	public static void ApplyRoll(State state, ERollDirection direction)
	{
		var basis = Roll(CurrentGridBasis(state), direction);
		ApplyGridBasis(state, basis);
	}

	public static void ApplyHeadingTurn(State state, EHeadingTurn turn)
	{
		var basis = HeadingTurn(CurrentGridBasis(state), turn);
		ApplyGridBasis(state, basis);
		state.MomentumLevel = System.Math.Max(state.MomentumLevel - 1, 0);
	}

	private static GridBasis CurrentGridBasis(State state) =>
		GridBasis.From(state.ForwardDirection, state.UpDirection, state.RightDirection);

	private static void ApplyGridBasis(State state, GridBasis basis)
	{
		state.ForwardDirection = basis.Forward;
		state.UpDirection = basis.Up;
		state.RightDirection = basis.Right;
	}
}
