namespace GrimSpace.World.StarSystem.Units;

public static class Factory
{
	public const string CargoShuttleId = "unit-cargo-shuttle";
	public const string ServiceVesselId = "unit-service-vessel";
	public const string PatrolId = "unit-patrol";

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

	public static IEnumerable<Unit> CreateDevUnits(
		string stationDockId,
		string planetADockId,
		string planetBDockId)
	{
		yield return Create(new Spawn(
			CargoShuttleId,
			EType.CargoShuttle,
			stationDockId,
			SpeedPerTick: 10,
			WorkDuration: 2,
			[planetADockId, stationDockId]));

		yield return Create(new Spawn(
			ServiceVesselId,
			EType.ServiceVessel,
			stationDockId,
			SpeedPerTick: 12,
			WorkDuration: 2,
			[planetBDockId, stationDockId]));

		yield return Create(new Spawn(
			PatrolId,
			EType.Patrol,
			stationDockId,
			SpeedPerTick: 14,
			WorkDuration: 1,
			[planetADockId, stationDockId, planetBDockId, stationDockId]));
	}
}
