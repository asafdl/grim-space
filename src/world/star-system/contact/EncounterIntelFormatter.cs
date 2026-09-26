using GrimSpace.World.StarSystem.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.World.StarSystem.Contact;

public static class EncounterIntelFormatter
{
	public static string FormatFleet(Fleet fleet)
	{
		ArgumentNullException.ThrowIfNull(fleet);
		if (fleet.Registrations.Count == 0)
			return "Unknown composition";

		var groups = fleet.Registrations
			.GroupBy(declaration => (declaration.Chassis, declaration.GearTier))
			.OrderBy(group => group.Key.Chassis)
			.ThenBy(group => group.Key.GearTier)
			.Select(group =>
				$"{group.Count()}× {DisplayName(group.Key.Chassis)} ({group.Key.GearTier})");

		return string.Join(", ", groups);
	}

	private static string DisplayName(BattleUnitType chassis) =>
		chassis switch
		{
			BattleUnitType.Patrol => "Patrol",
			BattleUnitType.Fighter => "Fighter",
			BattleUnitType.Carrier => "Carrier",
			BattleUnitType.Torpedo => "Torpedo",
			_ => chassis.ToString(),
		};
}
