using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Units;

public static class Factory
{
	public static Unit Create(Spawn spawn)
	{
		ArgumentNullException.ThrowIfNull(spawn);
		ArgumentException.ThrowIfNullOrEmpty(spawn.Id);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(spawn.SpeedPerTick, 0);
		ArgumentNullException.ThrowIfNull(spawn.ChoreDockIds);

		if (spawn.Type == EType.PirateFleet)
		{
			if (!string.IsNullOrEmpty(spawn.DockedAtDockId))
			{
				throw new ArgumentException(
					"Pirate fleets spawn in free space without a dock.",
					nameof(spawn));
			}

			if (spawn.ChoreDockIds.Count > 0)
			{
				throw new ArgumentException(
					"Pirate fleets do not use chore routes.",
					nameof(spawn));
			}

			var state = State.FromSpawn(spawn);
			state.Phase = EPhase.Docked;
			return new Unit(state);
		}

		ArgumentException.ThrowIfNullOrEmpty(spawn.DockedAtDockId);
		if (spawn.ChoreDockIds.Count == 0 && spawn.Type != EType.PlayerFleet)
		{
			throw new ArgumentException(
				"At least one chore destination is required.",
				nameof(spawn));
		}

		return new Unit(State.FromSpawn(spawn));
	}

	public static Unit CreatePirateFleet(
		string id,
		Coord idleCoord,
		EFaction faction,
		CombatProfile combatProfile) =>
		Create(new Spawn(
			id,
			EType.PirateFleet,
			"",
			idleCoord,
			UnitDefaults.SpeedPerTick(EType.PirateFleet),
			[],
			faction,
			combatProfile));
}
