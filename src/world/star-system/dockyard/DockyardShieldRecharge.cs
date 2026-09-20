using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Dockyard;

public static class DockyardShieldRecharge
{
	public const int CreditsPerPoint = 10;

	public static int TotalCurrent(ShipInstance ship)
	{
		var total = 0;
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			total += ship.ShieldPoints[face];
		return total;
	}

	public static int TotalMax(ShipInstance ship)
	{
		var total = 0;
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			total += ship.Spec.MaxShieldPoints[face];
		return total;
	}

	public static int MissingPoints(ShipInstance ship) =>
		System.Math.Max(0, TotalMax(ship) - TotalCurrent(ship));

	public static int MissingPointsOnFace(ShipInstance ship, ESpatialOrientation face)
	{
		var max = ship.Spec.MaxShieldPoints[face];
		if (max <= 0)
			return 0;

		var current = System.Math.Clamp(ship.ShieldPoints[face], 0, max);
		return max - current;
	}

	public static bool TryQuoteFace(ShipInstance ship, ESpatialOrientation face, out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		var missing = MissingPointsOnFace(ship, face);
		if (missing <= 0)
			return false;

		cost = ResourceBundle.Of(ResourceId.Credits, missing * CreditsPerPoint);
		return true;
	}

	public static bool TryApplyFace(ShipInstance ship, ESpatialOrientation face, out ShipInstance after)
	{
		after = null!;
		if (MissingPointsOnFace(ship, face) <= 0)
			return false;

		var shields = ship.ShieldPoints.Clone();
		shields[face] = ship.Spec.MaxShieldPoints[face];
		after = new ShipInstance(ship.Id, ship.Spec, ship.HullPoints, shields);
		return true;
	}

	public static bool TryQuote(ShipInstance ship, out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		var missing = MissingPoints(ship);
		if (missing <= 0)
			return false;

		cost = ResourceBundle.Of(ResourceId.Credits, missing * CreditsPerPoint);
		return true;
	}

	public static bool TryApply(ShipInstance ship, out ShipInstance after)
	{
		after = null!;
		if (MissingPoints(ship) <= 0)
			return false;

		var shields = ship.ShieldPoints.Clone();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			shields[face] = ship.Spec.MaxShieldPoints[face];

		after = new ShipInstance(ship.Id, ship.Spec, ship.HullPoints, shields);
		return true;
	}
}
