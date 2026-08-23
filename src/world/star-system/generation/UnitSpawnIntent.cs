using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Generation;

public sealed record UnitSpawnIntent(
	string Id,
	EType Type,
	string StartPoiId,
	IReadOnlyList<string> ChorePoiIds);
