using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Units;

public sealed class State
{
	public required string Id { get; init; }
	public required EType Type { get; init; }
	public string DockedAtDockId { get; set; } = "";
	public EPhase Phase { get; set; } = EPhase.Docked;
	public JourneyState Journey { get; } = new();
	public IReadOnlyList<string> ChoreDockIds { get; init; } = [];
	public int ChoreIndex { get; set; }
	public double SpeedPerTick { get; init; }
	public int WorkTicksRemaining { get; set; }

	public bool IsReadyToDepart =>
		Phase == EPhase.Docked && WorkTicksRemaining <= 0;

	public string NextChoreDockId() => ChoreDockIds[ChoreIndex];

	public void AdvanceChoreIndex() =>
		ChoreIndex = (ChoreIndex + 1) % ChoreDockIds.Count;

	internal void BeginTransit(string destinationDockId, TransitPath path)
	{
		Phase = EPhase.InTransit;
		Journey.DestinationDockId = destinationDockId;
		Journey.Path = path;
		Journey.LegIndex = 0;
		Journey.LegProgress = 0;
	}

	internal bool AdvanceTransit(double speedPerTick)
	{
		var path = Journey.Path
			?? throw new InvalidOperationException($"Unit '{Id}' is in transit without a path.");

		while (Journey.LegIndex < path.Legs.Length)
		{
			var leg = path.Legs[Journey.LegIndex];
			Journey.LegProgress += speedPerTick * leg.SpeedMultiplier;
			if (Journey.LegProgress < leg.Length)
				return false;

			Journey.LegProgress -= leg.Length;
			Journey.LegIndex++;
		}

		return true;
	}

	internal void ArriveAt(string dockId)
	{
		ClearTransit();
		DockedAtDockId = dockId;
		WorkTicksRemaining = 0;
	}

	internal void EnterWaiting() => Phase = EPhase.Waiting;

	internal void BeginWork(int duration)
	{
		Phase = EPhase.Working;
		WorkTicksRemaining = duration;
	}

	internal void CompleteWork()
	{
		Phase = EPhase.Docked;
		WorkTicksRemaining = 0;
	}

	internal void TickWork()
	{
		if (Phase != EPhase.Working || WorkTicksRemaining <= 0)
			return;

		WorkTicksRemaining--;
		if (WorkTicksRemaining <= 0)
			CompleteWork();
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
			DockedAtDockId = DockedAtDockId,
			Phase = Phase,
			ChoreDockIds = ChoreDockIds,
			ChoreIndex = ChoreIndex,
			SpeedPerTick = SpeedPerTick,
			WorkTicksRemaining = WorkTicksRemaining,
		};
		clone.Journey.DestinationDockId = Journey.DestinationDockId;
		clone.Journey.Path = Journey.Path;
		clone.Journey.LegIndex = Journey.LegIndex;
		clone.Journey.LegProgress = Journey.LegProgress;
		return clone;
	}

	public static State FromSpawn(Spawn spawn) =>
		new()
		{
			Id = spawn.Id,
			Type = spawn.Type,
			DockedAtDockId = spawn.DockedAtDockId,
			SpeedPerTick = spawn.SpeedPerTick,
			ChoreDockIds = spawn.ChoreDockIds,
		};
}
