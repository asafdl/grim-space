using GrimSpace.Battle.Objectives;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests;

internal static class OutcomeTestKit
{
	public static UnitStateHandoff Handoff(
		string id,
		EType chassis,
		int currentHullPoints,
		EShipGearTier gearTier = EShipGearTier.T0) =>
		new(
			id,
			chassis,
			currentHullPoints,
			ShipCatalog.NewRunLoadoutFor(chassis).BaseShieldPoints.Clone(),
			gearTier);

	public static EType ChassisFromShipId(string shipId)
	{
		var slug = shipId.Split('-', 2)[0];
		return slug switch
		{
			"fighter" => EType.Fighter,
			"carrier" => EType.Carrier,
			"repurposed" when shipId.StartsWith("repurposed-miner-", StringComparison.Ordinal) =>
				EType.RepurposedMiner,
			"void_bomb" => EType.VoidBomb,
			_ => throw new ArgumentException($"Unknown ship id prefix in '{shipId}'.", nameof(shipId)),
		};
	}
}
