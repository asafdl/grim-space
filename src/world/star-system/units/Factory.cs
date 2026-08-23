namespace GrimSpace.World.StarSystem.Units;

public static class Factory
{
	public static Unit Create(Spawn spawn)
	{
		ArgumentNullException.ThrowIfNull(spawn);
		ArgumentException.ThrowIfNullOrEmpty(spawn.Id);
		ArgumentException.ThrowIfNullOrEmpty(spawn.DockedAtDockId);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(spawn.SpeedPerTick, 0);
		ArgumentOutOfRangeException.ThrowIfNegative(spawn.WorkDuration);
		ArgumentNullException.ThrowIfNull(spawn.ChoreDockIds);
		if (spawn.ChoreDockIds.Count == 0)
			throw new ArgumentException("At least one chore destination is required.", nameof(spawn));

		return new Unit(State.FromSpawn(spawn));
	}
}
