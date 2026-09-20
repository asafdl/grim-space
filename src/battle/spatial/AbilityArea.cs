using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Spatial;

public static class AbilityArea
{
	public static HashSet<Coord> CellsInBounds(
		IAreaDamage area,
		BodyFrame frame,
		ESpatialOrientation direction,
		Func<Coord, bool> isInBounds)
	{
		var burstDirection = frame.Step(direction);
		var cells = area.GetArea(frame.Origin, burstDirection, frame.Fore, frame.Dorsal);
		var result = new HashSet<Coord>();
		foreach (var cell in cells)
		{
			if (isInBounds(cell))
				result.Add(cell);
		}

		return result;
	}

	public static HashSet<Coord> CellsInBounds(
		IAreaDamage area,
		BodyFrame frame,
		Func<Coord, bool> isInBounds) =>
		CellsInBounds(area, frame, ESpatialOrientation.Forward, isInBounds);
}
