using System.Globalization;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

public static class BattleDistance
{
	public const float MetersPerCell = 1000f;

	public static float SeparationMeters(Coord from, Coord to)
	{
		var offset = to - from;
		return MathF.Sqrt(
			offset.X * offset.X
			+ offset.Y * offset.Y
			+ offset.Z * offset.Z) * MetersPerCell;
	}

	public static string ApproximateCells(float cells) =>
		$"{Format(cells * MetersPerCell)}";

	public static string Format(float meters)
	{
		var roundedMeters = MathF.Round(meters / 10f) * 10f;
		if (roundedMeters >= 1000f)
		{
			var kilometers = MathF.Round(meters / 100f) / 10f;
			return $"{kilometers.ToString("0.#", CultureInfo.InvariantCulture)} km";
		}

		return $"{roundedMeters.ToString("0", CultureInfo.InvariantCulture)} m";
	}
}
