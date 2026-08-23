using GrimSpace.Core.Engine;
using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Generation;

public static class StarSystemBuilder
{
	private const int MinChainDistance = 120;
	private const double MinHubSeparationRadians = System.Math.PI / 3;
	private const int MaxPlacementAttempts = 256;
	private const int MaxLayoutAttempts = 64;
	private const int EdgePadding = 32;
	private const string PlacementTag = "poi-placement";

	public static StarMap Build(StarSystemBlueprint blueprint)
	{
		ArgumentNullException.ThrowIfNull(blueprint);

		InvalidOperationException? lastFailure = null;
		for (var layoutAttempt = 0; layoutAttempt < MaxLayoutAttempts; layoutAttempt++)
		{
			try
			{
				return BuildOnce(blueprint, layoutAttempt);
			}
			catch (InvalidOperationException ex)
			{
				lastFailure = ex;
			}
		}

		throw new InvalidOperationException(
			$"Could not generate supply map for seed {blueprint.Seed} after {MaxLayoutAttempts} layout attempts.",
			lastFailure);
	}

	private static StarMap BuildOnce(StarSystemBlueprint blueprint, int layoutAttempt)
	{
		var placed = PlacePois(blueprint, layoutAttempt);
		var pois = placed.Values.OrderBy(poi => poi.Id, StringComparer.Ordinal).ToArray();
		var star = pois.FirstOrDefault(poi => poi.Kind == EPointOfInterestKind.Star);

		var docksById = new Dictionary<string, Dock>(StringComparer.Ordinal);
		var docksByPoiId = new Dictionary<string, Dock>(StringComparer.Ordinal);
		foreach (var poi in pois.Where(poi => poi.Kind != EPointOfInterestKind.Star))
		{
			var neighbour = NeighbourForDock(poi, blueprint.SupplyPlan, placed);
			var dock = DockLayout.CreateDock(poi, neighbour, star);
			docksById[dock.Id] = dock;
			docksByPoiId[poi.Id] = dock;
		}

		var routePairs = blueprint.SupplyPlan.RouteConnections
			.Select(link => new RoutePair(
				docksByPoiId[link.FromPoiId].Id,
				docksByPoiId[link.ToPoiId].Id))
			.ToArray();

		var exclusions = pois
			.Select(poi => new CircularExclusion(
				poi.Center,
				poi.Kind == EPointOfInterestKind.Star ? poi.Radius + 30 : poi.Radius + 6,
				poi.Id))
			.ToArray();

		var routesById = RouteBuilder.Build(
			blueprint.Seed,
			blueprint.Width,
			blueprint.Height,
			docksById.Values,
			routePairs,
			exclusions,
			StarMap.DevRouteHalfWidth);

		var unitRegistry = new UnitRegistry();
		foreach (var intent in blueprint.UnitSpawns)
		{
			var spawn = new Spawn(
				intent.Id,
				intent.Type,
				docksByPoiId[intent.StartPoiId].Id,
				UnitDefaults.SpeedPerTick(intent.Type),
				UnitDefaults.WorkDuration(intent.Type),
				intent.ChorePoiIds.Select(poiId => docksByPoiId[poiId].Id).ToArray());
			unitRegistry.Add(Factory.Create(spawn));
		}

		Validate(blueprint, pois, docksByPoiId, routesById, unitRegistry);

		return new StarMap(
			blueprint,
			pois,
			new Timeline(),
			docksById,
			docksByPoiId,
			routesById,
			unitRegistry,
			new SystemTrafficController());
	}

	private static Dictionary<string, PointOfInterest> PlacePois(StarSystemBlueprint blueprint, int layoutAttempt)
	{
		var specsByRole = blueprint.Pois.ToDictionary(poi => poi.LogicalRole);
		var starSpec = blueprint.Pois.First(poi => poi.Id == SupplySystemPlan.StarPoiId);
		var refinerySpec = specsByRole[EPoiLogicalRole.Refinery];
		var extractionSpec = specsByRole[EPoiLogicalRole.Extraction];
		var storageSpec = specsByRole[EPoiLogicalRole.Storage];
		var exitSpec = specsByRole[EPoiLogicalRole.Exit];

		for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
		{
			var random = CreatePlacementRandom(blueprint.Seed, layoutAttempt, attempt);
			var refineryCenter = SampleFreeCenter(blueprint, refinerySpec.Radius, random);
			var extractionAngle = random.NextDouble() * System.Math.Tau;
			var extractionCenter = SampleAnnulus(
				blueprint,
				refineryCenter,
				extractionAngle,
				random,
				extractionSpec.Radius,
				MinChainDistance);
			if (extractionCenter is null)
				continue;

			var storageAngle = extractionAngle
				+ MinHubSeparationRadians
				+ random.NextDouble() * (System.Math.Tau - MinHubSeparationRadians);
			var storageCenter = SampleAnnulus(
				blueprint,
				refineryCenter,
				storageAngle,
				random,
				storageSpec.Radius,
				MinChainDistance);
			if (storageCenter is null)
				continue;

			var exitAngle = random.NextDouble() * System.Math.Tau;
			var exitCenter = SampleAnnulus(
				blueprint,
				storageCenter.Value,
				exitAngle,
				random,
				exitSpec.Radius,
				MinChainDistance);
			if (exitCenter is null)
				continue;

			var candidates = new PointOfInterest[]
			{
				extractionSpec.Place(extractionCenter.Value),
				refinerySpec.Place(refineryCenter),
				storageSpec.Place(storageCenter.Value),
				exitSpec.Place(exitCenter.Value),
			};

			var starCenter = SampleStarCenter(blueprint, starSpec.Radius, random);
			var allPois = candidates.Append(starSpec.Place(starCenter)).ToArray();
			if (!IsValidLayout(allPois, blueprint))
				continue;

			return allPois.ToDictionary(poi => poi.Id, StringComparer.Ordinal);
		}

		throw new InvalidOperationException(
			$"Could not place POIs for seed {blueprint.Seed} layout {layoutAttempt}.");
	}

	private static Coord? SampleAnnulus(
		StarSystemBlueprint blueprint,
		Coord anchor,
		double angle,
		StableRandom random,
		int poiRadius,
		double minDistance)
	{
		var maxDistance = MaxDistanceAlongRay(blueprint, anchor, angle, poiRadius);
		if (maxDistance < minDistance)
			return null;

		var distance = minDistance + random.NextDouble() * (maxDistance - minDistance);
		return new Coord(
			(int)System.Math.Round(anchor.X + System.Math.Cos(angle) * distance),
			0,
			(int)System.Math.Round(anchor.Z + System.Math.Sin(angle) * distance));
	}

	private static double MaxDistanceAlongRay(
		StarSystemBlueprint blueprint,
		Coord anchor,
		double angle,
		int poiRadius)
	{
		var cos = System.Math.Cos(angle);
		var sin = System.Math.Sin(angle);
		var maxDistance = double.PositiveInfinity;

		if (cos > 1e-9)
			maxDistance = System.Math.Min(maxDistance, (blueprint.Width - poiRadius - anchor.X) / cos);
		else if (cos < -1e-9)
			maxDistance = System.Math.Min(maxDistance, (poiRadius - anchor.X) / cos);

		if (sin > 1e-9)
			maxDistance = System.Math.Min(maxDistance, (blueprint.Height - poiRadius - anchor.Z) / sin);
		else if (sin < -1e-9)
			maxDistance = System.Math.Min(maxDistance, (poiRadius - anchor.Z) / sin);

		return maxDistance;
	}

	private static StableRandom CreatePlacementRandom(int seed, int layoutAttempt, int attempt) =>
		new(
			StableSeedMixer.From(seed)
				.Add(PlacementTag)
				.Add(layoutAttempt)
				.Add(attempt)
				.Value);

	private static Coord SampleFreeCenter(StarSystemBlueprint blueprint, int radius, StableRandom random)
	{
		var margin = radius + EdgePadding;
		var x = margin + (int)(random.NextDouble() * (blueprint.Width - 2 * margin));
		var z = margin + (int)(random.NextDouble() * (blueprint.Height - 2 * margin));
		return new Coord(x, 0, z);
	}

	private static Coord SampleStarCenter(StarSystemBlueprint blueprint, int radius, StableRandom random)
	{
		var margin = radius + EdgePadding;
		var minX = System.Math.Max(margin, (int)(blueprint.Width * 0.30));
		var maxX = System.Math.Min(blueprint.Width - margin - 1, (int)(blueprint.Width * 0.70));
		var minZ = System.Math.Max(margin, (int)(blueprint.Height * 0.30));
		var maxZ = System.Math.Min(blueprint.Height - margin - 1, (int)(blueprint.Height * 0.70));
		var x = minX + (int)(random.NextDouble() * (maxX - minX + 1));
		var z = minZ + (int)(random.NextDouble() * (maxZ - minZ + 1));
		return new Coord(x, 0, z);
	}

	private static bool IsValidLayout(IReadOnlyList<PointOfInterest> pois, StarSystemBlueprint blueprint)
	{
		foreach (var poi in pois)
		{
			if (!GridBounds.IsCircleWhollyInRectangle(poi.Center, poi.Radius, blueprint.Width, blueprint.Height))
				return false;
		}

		for (var i = 0; i < pois.Count; i++)
		{
			for (var j = i + 1; j < pois.Count; j++)
			{
				if (StarMap.PoisOverlap(pois[i], pois[j]))
					return false;
			}
		}

		foreach (var (fromPoiId, toPoiId) in blueprint.SupplyPlan.RouteConnections)
		{
			var from = pois.First(poi => poi.Id == fromPoiId);
			var to = pois.First(poi => poi.Id == toPoiId);
			var distance = GridDistance(from.Center, to.Center);
			if (distance < MinChainDistance)
				return false;
		}

		return true;
	}

	private static double GridDistance(Coord a, Coord b)
	{
		var dx = a.X - b.X;
		var dz = a.Z - b.Z;
		return System.Math.Sqrt(dx * dx + dz * dz);
	}

	private static PointOfInterest NeighbourForDock(
		PointOfInterest poi,
		SupplySystemPlan plan,
		IReadOnlyDictionary<string, PointOfInterest> placed) =>
		poi.LogicalRole switch
		{
			EPoiLogicalRole.Extraction => placed[plan.RefineryPoiId],
			EPoiLogicalRole.Refinery => placed[plan.StoragePoiId],
			EPoiLogicalRole.Storage => placed[plan.ExitPoiId],
			EPoiLogicalRole.Exit => placed[plan.StoragePoiId],
			_ => throw new InvalidOperationException($"POI '{poi.Id}' does not receive a dock."),
		};

	private static void Validate(
		StarSystemBlueprint blueprint,
		IReadOnlyList<PointOfInterest> pois,
		IReadOnlyDictionary<string, Dock> docksByPoiId,
		IReadOnlyDictionary<string, SpaceRoute> routesById,
		UnitRegistry unitRegistry)
	{
		var plan = blueprint.SupplyPlan;
		var roles = pois
			.Where(poi => poi.LogicalRole is not null and not EPoiLogicalRole.Environment)
			.GroupBy(poi => poi.LogicalRole!.Value)
			.ToDictionary(group => group.Key, group => group.Single());

		if (roles.Count != 4)
			throw new InvalidOperationException("Supply map must contain exactly four operational POI roles.");

		foreach (var spec in blueprint.Pois)
		{
			if (!pois.Any(poi => poi.Id == spec.Id))
				throw new InvalidOperationException($"Blueprint POI '{spec.Id}' was not placed.");
		}

		foreach (var (fromPoiId, toPoiId) in plan.RouteConnections)
		{
			var fromDock = docksByPoiId[fromPoiId];
			var toDock = docksByPoiId[toPoiId];
			var routeId = string.Compare(fromDock.Id, toDock.Id, StringComparison.Ordinal) <= 0
				? $"route:{fromDock.Id}:{toDock.Id}"
				: $"route:{toDock.Id}:{fromDock.Id}";

			if (!routesById.ContainsKey(routeId))
				throw new InvalidOperationException($"Missing route for POI link {fromPoiId} <-> {toPoiId}.");
		}

		if (docksByPoiId.Count != 4)
			throw new InvalidOperationException("Supply map must contain exactly four docks.");

		if (unitRegistry.Ids.Count() != blueprint.UnitSpawns.Count)
			throw new InvalidOperationException("Unit spawn count does not match blueprint.");
	}
}
