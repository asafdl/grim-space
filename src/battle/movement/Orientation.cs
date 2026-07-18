using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

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

	public static int ApCostForNetYaw(int netQuarters) =>
		NormalizeQuarters(netQuarters) switch
		{
			0 => 0,
			2 => CombatConfig.HeadingTurn180ApCost,
			_ => CombatConfig.HeadingTurn90ApCost,
		};

	public static int MomentumLossForNetYaw(int netQuarters) => ApCostForNetYaw(netQuarters);

	public static GridBasis ApplyNetYaw(GridBasis basis, int netQuarters) =>
		NormalizeQuarters(netQuarters) switch
		{
			0 => basis,
			1 => HeadingTurn(basis, EHeadingTurn.YawRight),
			2 => HeadingTurn(basis, EHeadingTurn.Yaw180),
			3 => HeadingTurn(basis, EHeadingTurn.YawLeft),
			_ => basis,
		};

	private static GridBasis CurrentGridBasis(State state) =>
		GridBasis.From(state.Fore, state.Dorsal, state.Starboard);

	private static void ApplyGridBasis(State state, GridBasis basis)
	{
		state.Fore = basis.Forward;
		state.Dorsal = basis.Up;
		state.Starboard = basis.Right;
	}
}
