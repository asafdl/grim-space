using GrimSpace.Battle.Objectives;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests;

internal static class OutcomeTestKit
{
	public static UnitStateHandoff Handoff(string id, EType chassis, int hullPoints) =>
		new(id, chassis, hullPoints, ShipCatalog.DefaultFor(chassis).MaxShieldPoints.Clone());

	public static EType ChassisFromShipId(string shipId)
	{
		var slug = shipId.Split('-', 2)[0];
		return slug switch
		{
			"fighter" => EType.Fighter,
			"carrier" => EType.Carrier,
			"patrol" => EType.Patrol,
			"torpedo" => EType.Torpedo,
			_ => throw new ArgumentException($"Unknown ship id prefix in '{shipId}'.", nameof(shipId)),
		};
	}
}
