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

	public static GridBasis? SelectHeading(
		IReadOnlyList<GridBasis> reachableBases,
		GridBasis current,
		Coord heading)
	{
		var currentRoll = RollQuarters(current);
		return reachableBases
			.Where(basis => basis.Forward == heading)
			.OrderBy(basis => RollDistance(currentRoll, RollQuarters(basis)))
			.ThenBy(RollQuarters)
			.Cast<GridBasis?>()
			.FirstOrDefault();
	}

	public static GridBasis? CycleRoll(
		IReadOnlyList<GridBasis> reachableBases,
		GridBasis current,
		int delta)
	{
		var rolls = reachableBases
			.Where(basis => basis.Forward == current.Forward)
			.Distinct()
			.OrderBy(RollQuarters)
			.ToList();
		if (rolls.Count == 0)
			return null;

		var currentIndex = rolls.IndexOf(current);
		if (currentIndex < 0)
			return rolls[0];

		var direction = System.Math.Sign(delta);
		if (direction == 0 || rolls.Count == 1)
			return current;

		return rolls[(currentIndex + direction + rolls.Count) % rolls.Count];
	}

	private static int RollDistance(int left, int right)
	{
		var distance = System.Math.Abs(left - right);
		return System.Math.Min(distance, 4 - distance);
	}
}
