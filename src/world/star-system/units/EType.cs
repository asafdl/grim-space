namespace GrimSpace.World.StarSystem.Units;

// TODO: EType overloads ship/traffic archetypes (MiningBarge, CargoShuttle) with map-token
// categories (PlayerFleet, PirateFleet). Encounter identity belongs on EFaction + CombatProfile;
// consider splitting into EMapPresence vs EShipClass, or renaming PirateFleet → Encounter.
public enum EType
{
	CargoShuttle,
	ComplianceVessel,
	ServiceVessel,
	Patrol,
	MiningBarge,
	RefineryHauler,
	ExportFreighter,
	PlayerFleet,
	PirateFleet,
}
