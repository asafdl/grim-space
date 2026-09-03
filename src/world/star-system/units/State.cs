using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Units;

public sealed class State
{
	public required string Id { get; init; }
	public required EType Type { get; init; }
	public EFaction Faction { get; init; } = EFaction.TheOptimality;
	public CombatProfile? CombatProfile { get; init; }
	public string DockedAtDockId { get; set; } = "";
	public Coord IdleCoord { get; set; }
	public EPhase Phase { get; set; } = EPhase.Docked;
	public JourneyState Journey { get; } = new();
	public IReadOnlyList<string> ChoreDockIds { get; init; } = [];
	public int ChoreIndex { get; set; }
	public double SpeedPerTick { get; init; }
	public int WorkStartTick { get; set; }
	internal string? SpawnWorkPoiId { get; set; }
	internal int SpawnWorkRemainingTicks { get; set; }

	public bool IsReadyToDepart =>
		!string.IsNullOrEmpty(DockedAtDockId)
		&& Phase == EPhase.Docked
		&& ChoreDockIds.Count > 0;

	public string NextChoreDockId() => ChoreDockIds[ChoreIndex];

	public void AdvanceChoreIndex() =>
		ChoreIndex = (ChoreIndex + 1) % ChoreDockIds.Count;

	public (Coord Position, Coord? Tangent) CommittedPosition(
		StarMap world,
		TransitPath? path,
		float tickFraction)
	{
		if (Phase == EPhase.InTransit && Journey.IsActive)
		{
			var transitPath = path
				?? throw new InvalidOperationException(
					$"Unit '{Id}' is in transit without a cached path.");
			var elapsed = world.Timeline.Clock.Current - Journey.StartTick + tickFraction;
			var (position, tangent) = Journey.SamplePosition(path, elapsed, SpeedPerTick);
			return (position, tangent);
		}

		if (!string.IsNullOrEmpty(DockedAtDockId))
			return (world.DocksById[DockedAtDockId].Position, null);

		return (IdleCoord, null);
	}

	internal void StartJourney(
		long journeyId,
		Coord origin,
		Coord destination,
		int startTick)
	{
		Phase = EPhase.InTransit;
		Journey.JourneyId = journeyId;
		Journey.Origin = origin;
		Journey.Destination = destination;
		Journey.StartTick = startTick;
	}

	internal void ArriveAt(string dockId)
	{
		ClearTransit();
		DockedAtDockId = dockId;
	}

	internal void ArriveAtFreeSpace(Coord coord)
	{
		ClearTransit();
		DockedAtDockId = "";
		IdleCoord = coord;
	}

	internal void BeginWork(int startTick)
	{
		Phase = EPhase.Working;
		WorkStartTick = startTick;
	}

	internal void CompleteWork()
	{
		Phase = EPhase.Docked;
		WorkStartTick = 0;
	}

	internal void ClearTransit()
	{
		Phase = EPhase.Docked;
		Journey.Clear();
	}

	public State Clone()
	{
		var clone = new State
		{
			Id = Id,
			Type = Type,
			Faction = Faction,
			CombatProfile = CombatProfile,
			DockedAtDockId = DockedAtDockId,
			IdleCoord = IdleCoord,
			Phase = Phase,
			ChoreDockIds = ChoreDockIds,
			ChoreIndex = ChoreIndex,
			SpeedPerTick = SpeedPerTick,
			WorkStartTick = WorkStartTick,
			SpawnWorkPoiId = SpawnWorkPoiId,
			SpawnWorkRemainingTicks = SpawnWorkRemainingTicks,
		};
		clone.Journey.JourneyId = Journey.JourneyId;
		clone.Journey.Origin = Journey.Origin;
		clone.Journey.Destination = Journey.Destination;
		clone.Journey.StartTick = Journey.StartTick;
		return clone;
	}

	public static State FromSpawn(Spawn spawn) =>
		new()
		{
			Id = spawn.Id,
			Type = spawn.Type,
			Faction = spawn.Faction,
			CombatProfile = spawn.CombatProfile,
			DockedAtDockId = spawn.DockedAtDockId,
			IdleCoord = spawn.IdleCoord,
			SpeedPerTick = spawn.SpeedPerTick,
			ChoreDockIds = spawn.ChoreDockIds,
		};
}
