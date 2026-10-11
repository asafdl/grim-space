namespace GrimSpace.Run.Persistence;

public sealed record PilotPersistenceDto(
	string Id,
	string Name,
	string PortraitId,
	string AssignedShipId);
