using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Units;

public sealed record FleetSpawned(
	string FleetId,
	IReadOnlyList<ShipSpawnDeclaration> Members);
