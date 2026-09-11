using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Movement;

public readonly record struct MoveTransition(
	GridBasis HeadingBasis,
	Coord Destination,
	GridBasis ArrivalBasis);

public static class Orientation
{
	public static MoveTransition MoveStep(
		Coord position,
		GridBasis basis,
		EHeadingTurn? heading,
		ERollDirection? roll)
	{
		var headingBasis = heading is { } headingTurn
			? HeadingTurn(basis, headingTurn)
			: basis;
		var arrivalBasis = roll is { } rollDirection
			? Roll(headingBasis, rollDirection)
			: headingBasis;
		return new MoveTransition(
			headingBasis,
			position + headingBasis.Forward,
			arrivalBasis);
	}

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
			EHeadingTurn.Yaw180 => new GridBasis(
				-basis.Forward,
				basis.Up,
				-basis.Right),
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
	}

	public static bool IsYawTurn(EHeadingTurn turn) =>
		turn is EHeadingTurn.YawLeft or EHeadingTurn.YawRight or EHeadingTurn.Yaw180;

	public static int NormalizeQuarters(int quarters) => ((quarters % 4) + 4) % 4;

	private static GridBasis CurrentGridBasis(State state) =>
		GridBasis.From(state.Fore, state.Dorsal, state.Starboard);

	private static void ApplyGridBasis(State state, GridBasis basis)
	{
		state.Fore = basis.Forward;
		state.Dorsal = basis.Up;
		state.Starboard = basis.Right;
	}
}
