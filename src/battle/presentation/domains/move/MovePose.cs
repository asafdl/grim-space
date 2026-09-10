using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

public static class MovePose
{
	private static readonly Coord[] CardinalHeadings =
	[
		new Coord(1, 0, 0),
		new Coord(-1, 0, 0),
		Coord.Up,
		Coord.Zero - Coord.Up,
		Coord.Forward,
		Coord.Zero - Coord.Forward,
	];

	public static IReadOnlyList<Coord> Headings => CardinalHeadings;

	public static GridBasis For(Coord forward, int clockwiseRollQuarters)
	{
		if (!CardinalHeadings.Contains(forward))
			throw new ArgumentOutOfRangeException(nameof(forward));

		var dorsal = forward == Coord.Up || forward == Coord.Zero - Coord.Up
			? Coord.Forward
			: Coord.Up;
		var basis = GridBasis.From(forward, dorsal, Coord.Cross(dorsal, forward));
		for (var i = 0; i < Orientation.NormalizeQuarters(clockwiseRollQuarters); i++)
			basis = Orientation.Roll(basis, Movement.Enums.ERollDirection.Clockwise);
		return basis;
	}

	public static int RollQuarters(GridBasis basis)
	{
		for (var roll = 0; roll < 4; roll++)
		{
			if (For(basis.Forward, roll) == basis)
				return roll;
		}

		throw new ArgumentException("Basis is not a cardinal ship orientation.", nameof(basis));
	}
}
