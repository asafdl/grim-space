namespace GrimSpace.World.StarSystem.Contact;

public sealed record FleetPursuitChanged(
	string HunterFleetId,
	string TargetFleetId,
	bool IsActive);
