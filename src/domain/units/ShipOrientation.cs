using GrimSpace.Domain.Grid;
using GrimSpace.Domain.Units.Enums;

namespace GrimSpace.Domain.Units;

public readonly record struct ShipBasis(Coord Forward, Coord Up, Coord Right)
{
	public static ShipBasis From(Coord forward, Coord up, Coord right) =>
		new(forward, up, right);
}

public static class ShipOrientation
{
	public static ShipBasis Roll(ShipBasis basis, ERollDirection direction) =>
		direction switch
		{
			ERollDirection.Clockwise => new ShipBasis(
				basis.Forward,
				-basis.Right,
				basis.Up),
			ERollDirection.CounterClockwise => new ShipBasis(
				basis.Forward,
				basis.Right,
				-basis.Up),
			_ => throw new ArgumentOutOfRangeException(nameof(direction)),
		};

	public static ShipBasis HeadingTurn(ShipBasis basis, EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.YawRight => new ShipBasis(
				basis.Right,
				basis.Up,
				-basis.Forward),
			EHeadingTurn.YawLeft => new ShipBasis(
				-basis.Right,
				basis.Up,
				basis.Forward),
			EHeadingTurn.PitchUp => new ShipBasis(
				basis.Up,
				-basis.Forward,
				basis.Right),
			EHeadingTurn.PitchDown => new ShipBasis(
				-basis.Up,
				basis.Forward,
				basis.Right),
			_ => throw new ArgumentOutOfRangeException(nameof(turn)),
		};
}
