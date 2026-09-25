using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public sealed class StarMap : IWorld<StarMap>, IActorWorld, IActorStateWorld<State, StarMap>
{
	public const int MapWidth = 1024;
	public const int MapHeight = 1024;
	public const int RouteHalfWidth = 24;

	public StarSystemBlueprint Blueprint { get; }
	public EFaction ControllingFaction => Blueprint.ControllingFaction;
	public int Seed => Blueprint.Seed;
	public int Width => Blueprint.Width;
	public int Height => Blueprint.Height;
	public IReadOnlyList<PointOfInterest> PointsOfInterest { get; }
	public IReadOnlyList<NavigationLandmark> NavigationLandmarks { get; }
	public Timeline Timeline { get; }
	public IReadOnlyDictionary<string, Dock> DocksById { get; }
	public IReadOnlyDictionary<string, Dock> DocksByPoiId { get; }
	public IReadOnlyDictionary<Coord, Dock> DocksByPosition { get; }
	public IReadOnlyDictionary<string, SpaceRoute> RoutesById { get; }
	public FleetRegistry FleetRegistry { get; }
	public IEnumerable<string> ActorIds
	{
		get
		{
			yield return StarSystemActorIds.Contracts;
			foreach (var id in FleetRegistry.Ids)
				yield return id;
		}
	}
	public ContractRegistry ContractRegistry { get; }
	public StoryObjectiveRegistry StoryObjectives { get; }
	public PlayerResources PlayerResources { get; }
	public IShipRegistryReader? ShipRegistryReader { get; }
	public PathfindingTerrain PathfindingTerrain { get; }

	public bool WaitingForPlayerInput { get; internal set; }

	public string? ActiveNarrativeId { get; internal set; }

	public State StateOf(string unitId) => FleetRegistry.FleetOf(unitId).State;

	internal StarMap(
		StarSystemBlueprint blueprint,
		IReadOnlyList<PointOfInterest> pointsOfInterest,
		IReadOnlyList<NavigationLandmark> navigationLandmarks,
		Timeline timeline,
		IReadOnlyDictionary<string, Dock> docksById,
		IReadOnlyDictionary<string, Dock> docksByPoiId,
		IReadOnlyDictionary<string, SpaceRoute> routesById,
		FleetRegistry fleetRegistry,
		ContractRegistry contractRegistry,
		StoryObjectiveRegistry storyObjectives,
		PlayerResources playerResources,
		PathfindingTerrain pathfindingTerrain,
		IShipRegistryReader? shipRegistryReader = null,
		bool waitingForPlayerInput = false,
		string? activeNarrativeId = null)
	{
		Blueprint = blueprint;
		PointsOfInterest = pointsOfInterest;
		NavigationLandmarks = navigationLandmarks;
		Timeline = timeline;
		DocksById = docksById;
		DocksByPoiId = docksByPoiId;
		DocksByPosition = docksById.Values
			.GroupBy(dock => dock.Position)
			.ToDictionary(group => group.Key, group => group.Single());
		RoutesById = routesById;
		FleetRegistry = fleetRegistry;
		ContractRegistry = contractRegistry;
		StoryObjectives = storyObjectives;
		PlayerResources = playerResources;
		PathfindingTerrain = pathfindingTerrain;
		ShipRegistryReader = shipRegistryReader;
		WaitingForPlayerInput = waitingForPlayerInput;
		ActiveNarrativeId = activeNarrativeId;
	}

	public bool IsInBounds(Coord point) =>
		point.Y == 0
		&& point.X >= 0 && point.X < Width
		&& point.Z >= 0 && point.Z < Height;

	public bool TryGetPointOfInterest(string poiId, out PointOfInterest poi)
	{
		poi = PointsOfInterest.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, poiId, StringComparison.Ordinal))!;
		return poi is not null;
	}

	public PointOfInterest GetPointOfInterest(string poiId)
	{
		if (!TryGetPointOfInterest(poiId, out var poi))
			throw new InvalidOperationException($"Unknown POI '{poiId}'.");

		return poi;
	}

	public StarMap Fork() => Fork(Timeline.Clone());

	public StarMap ForkForSimulation() => Fork(Timeline.CloneSnapshot());

	private StarMap Fork(Timeline timeline) =>
		new(
			Blueprint,
			PointsOfInterest.Select(poi => poi.Fork()).ToList(),
			NavigationLandmarks,
			timeline,
			DocksById,
			DocksByPoiId,
			RoutesById,
			FleetRegistry.CloneForFork(),
			ContractRegistry.CloneForFork(),
			StoryObjectives.CloneForFork(),
			PlayerResources.CloneForFork(),
			PathfindingTerrain,
			ShipRegistryReader,
			WaitingForPlayerInput,
			ActiveNarrativeId);

	public static bool PoisOverlap(PointOfInterest a, PointOfInterest b)
	{
		var dx = a.PlacedCenter.X - b.PlacedCenter.X;
		var dz = a.PlacedCenter.Z - b.PlacedCenter.Z;
		var distanceSquared = (long)dx * dx + (long)dz * dz;
		var combined = a.Radius + b.Radius;
		return distanceSquared < (long)combined * combined;
	}

	public static StarMap Create(int seed = 0, IShipRegistryReader? shipRegistryReader = null) =>
		StarSystemGenerator.Generate(seed, EStarSystemClass.Supply, shipRegistryReader);
}
