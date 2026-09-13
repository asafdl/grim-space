using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Units;

public static class Factory
{
	public static Fleet Create(Spawn spawn, IReadOnlyList<FleetMember>? members = null)
	{
		ArgumentNullException.ThrowIfNull(spawn);
		ArgumentException.ThrowIfNullOrEmpty(spawn.Id);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(spawn.SpeedPerTick, 0);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(spawn.EngageRadius, 0);
		ArgumentNullException.ThrowIfNull(spawn.ChoreDockIds);

		return new Fleet(State.FromSpawn(spawn), members);
	}
}
