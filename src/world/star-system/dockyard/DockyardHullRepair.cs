using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Dockyard;

public static class DockyardHullRepair
{
	public const int CreditCost = 20;
	public const int ScrapCost = 20;

	public static int MissingHull(ShipInstance ship) =>
		System.Math.Max(0, ship.Spec.MaxHullPoints - ship.HullPoints);

	public static bool TryQuote(ShipInstance ship, out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		if (MissingHull(ship) <= 0)
			return false;

		cost = ResourceBundle.Create(
			(ResourceId.Credits, CreditCost),
			(ResourceId.ScrapAlloy, ScrapCost));
		return true;
	}

	public static bool TryApply(ShipInstance ship, out ShipInstance after)
	{
		after = null!;
		if (MissingHull(ship) <= 0)
			return false;

		after = new ShipInstance(ship.Id, ship.Spec, ship.Spec.MaxHullPoints, ship.ShieldPoints.Clone());
		return true;
	}
}
