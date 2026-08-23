namespace GrimSpace.World.StarSystem.Units;

public sealed record Spawn(
	string Id,
	EType Type,
	string DockedAtDockId,
	double SpeedPerTick,
	int WorkDuration,
	IReadOnlyList<string> ChoreDockIds);
