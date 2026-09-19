using GrimSpace.Units;

namespace GrimSpace.Run;

public sealed record FleetSpawned(
	string FleetId,
	IReadOnlyList<ShipSpawnDeclaration> Members);
