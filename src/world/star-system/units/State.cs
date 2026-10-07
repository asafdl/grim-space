using GrimSpace.Battle.Objectives;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.FleetSpawner;

namespace GrimSpace.World.StarSystem.Units;

public sealed class State
{
	public required string Id { get; init; }
	public required EType Type { get; init; }
	public EFaction Faction { get; init; } = EFaction.TheOptimality;
	public int AggressionRating { get; init; }
	public FleetTravel Travel { get; internal set; } = new FleetTravel.AtRest(default);
	public Coord PatrolOrigin { get; init; }
	public int PatrolRadius { get; init; }
	public IReadOnlyList<string> ChoreDockIds { get; init; } = [];
	public int ChoreIndex { get; set; }
	public double SpeedPerTick { get; init; }
	public double EngageRadius { get; init; }
	public double VisionRadius { get; init; }
	public Engagement? CurrentEngagement { get; internal set; }
	public TravelTarget TravelTarget { get; set; } = TravelTarget.None;
	public string PendingWreckContractId { get; set; } = "";
	public EFleetSpawnerSource SpawnerSource { get; set; }
	public int? FleetSpawnerExpiresAtTick { get; set; }
	public string? SourceContractId { get; set; }
	public FleetPursuitDirective? PursuitDirective { get; set; }

	public string NextChoreDockId() => ChoreDockIds[ChoreIndex];

	public void AdvanceChoreIndex() =>
		ChoreIndex = (ChoreIndex + 1) % ChoreDockIds.Count;

	public (Coord Position, Coord? Tangent) PositionAt(
		StarMap world,
		TransitPath? path,
		float tickFraction)
	{
		if (Travel is FleetTravel.Journey journey)
		{
			var transitPath = path
				?? throw new InvalidOperationException(
					$"Fleet '{Id}' is in transit without a cached path.");
			var elapsed = world.Timeline.Clock.Current - journey.StartTick + tickFraction;
			var (position, tangent) = transitPath.SampleAtElapsed(elapsed, SpeedPerTick);
			return (position, tangent);
		}

		return (((FleetTravel.AtRest)Travel).Position, null);
	}

	public PiecewiseRouteSample? PositionContinuousAt(
		StarMap world,
		TransitPath? path,
		float tickFraction)
	{
		if (Travel is not FleetTravel.Journey journey)
			return null;

		var transitPath = path
			?? throw new InvalidOperationException(
				$"Fleet '{Id}' is in transit without a cached path.");
		var elapsed = world.Timeline.Clock.Current - journey.StartTick + tickFraction;
		return transitPath.SampleContinuousAtElapsed(elapsed, SpeedPerTick);
	}

	internal void StartJourney(
		long journeyId,
		Coord origin,
		Coord destination,
		int startTick) =>
		Travel = new FleetTravel.Journey(journeyId, origin, destination, startTick);

	internal void StopAt(Coord position) =>
		Travel = new FleetTravel.AtRest(position);

	public State Clone()
	{
		var clone = new State
		{
			Id = Id,
			Type = Type,
			Faction = Faction,
			AggressionRating = AggressionRating,
			Travel = Travel,
			PatrolOrigin = PatrolOrigin,
			PatrolRadius = PatrolRadius,
			ChoreDockIds = ChoreDockIds,
			ChoreIndex = ChoreIndex,
			SpeedPerTick = SpeedPerTick,
			EngageRadius = EngageRadius,
			VisionRadius = VisionRadius,
			SpawnerSource = SpawnerSource,
			FleetSpawnerExpiresAtTick = FleetSpawnerExpiresAtTick,
			SourceContractId = SourceContractId,
			PursuitDirective = PursuitDirective,
		};
		clone.CurrentEngagement = CurrentEngagement is null
			? null
			: CurrentEngagement with
			{
				EngagementParticipantIds =
					new HashSet<string>(CurrentEngagement.EngagementParticipantIds, StringComparer.Ordinal),
			};
		clone.TravelTarget = TravelTarget;
		clone.PendingWreckContractId = PendingWreckContractId;
		return clone;
	}

	public static State FromSpawn(Spawn spawn) =>
		new()
		{
			Id = spawn.Id,
			Type = spawn.Type,
			Faction = spawn.Faction,
			AggressionRating = spawn.AggressionRating,
			Travel = new FleetTravel.AtRest(spawn.Position),
			PatrolOrigin = spawn.Position,
			PatrolRadius = spawn.PatrolRadius,
			SpeedPerTick = spawn.SpeedPerTick,
			EngageRadius = spawn.EngageRadius,
			VisionRadius = spawn.VisionRadius,
			ChoreDockIds = spawn.ChoreDockIds,
			SourceContractId = spawn.SourceContractId,
		};
}

/// <summary>
/// Ambient spawner fleet chasing a delivery holder without <see cref="State.SourceContractId"/>.
/// </summary>
public sealed record FleetPursuitDirective(string ContractId, string TargetFleetId);
