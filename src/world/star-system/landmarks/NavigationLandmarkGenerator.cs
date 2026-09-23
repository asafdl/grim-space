using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.World.StarSystem.Landmarks;

public static class NavigationLandmarkGenerator
{
	private const string ContentTag = "navigation-landmark-content";
	private const string PlacementTag = "navigation-landmark-placement";
	private const int GenericSlotCount = 4;
	private const int SystemSlotCount = 2;
	private const int ExtraSlotCount = 1;
	private const double TopBandFraction = 0.15;

	public static IReadOnlyList<NavigationLandmark> Generate(
		StarSystemBlueprint blueprint,
		int layoutAttempt,
		IReadOnlyList<PointOfInterest> pois,
		IEnumerable<SpaceRoute> routes)
	{
		ArgumentNullException.ThrowIfNull(blueprint);
		ArgumentNullException.ThrowIfNull(pois);
		ArgumentNullException.ThrowIfNull(routes);

		var profile = blueprint.NavigationLandmarkProfile;
		if (profile.TargetCount <= 0)
			return [];

		var routeList = routes.ToArray();
		var selections = SelectContent(blueprint.Seed, layoutAttempt, profile);
		if (selections.Count > profile.MaximumCount)
			selections = selections.Take(profile.MaximumCount).ToList();

		var placed = new List<NavigationLandmark>();
		var idOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
		for (var index = 0; index < selections.Count; index++)
		{
			var selection = selections[index];
			if (!TryPlace(
					blueprint,
					layoutAttempt,
					index,
					selection,
					pois,
					routeList,
					profile,
					placed,
					idOrdinals,
					out var landmark))
			{
				continue;
			}

			placed.Add(landmark);
		}

		if (placed.Count < profile.MinimumCount)
		{
			throw new InvalidOperationException(
				$"Could not place navigation landmarks for seed {blueprint.Seed} layout {layoutAttempt}: "
				+ $"requested at least {profile.MinimumCount}, placed {placed.Count}.");
		}

		return placed;
	}

	private sealed record SelectedEntry(NavigationLandmarkPoolEntry Entry, string DisplayName);

	private static List<SelectedEntry> SelectContent(
		int seed,
		int layoutAttempt,
		NavigationLandmarkGenerationProfile profile)
	{
		var random = CreateRandom(seed, layoutAttempt, ContentTag);
		var slugCounts = new Dictionary<string, int>(StringComparer.Ordinal);
		var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var selections = new List<SelectedEntry>();

		for (var i = 0; i < GenericSlotCount; i++)
			TryAddSelection(profile.GenericPool, random, slugCounts, usedNames, selections);

		for (var i = 0; i < SystemSlotCount; i++)
			TryAddSelection(profile.SystemSpecificPool, random, slugCounts, usedNames, selections);

		for (var i = 0; i < ExtraSlotCount; i++)
		{
			var combined = profile.GenericPool
				.Concat(profile.SystemSpecificPool)
				.OrderBy(entry => entry.Slug, StringComparer.Ordinal)
				.ToArray();
			TryAddSelection(combined, random, slugCounts, usedNames, selections);
		}

		while (selections.Count < profile.TargetCount)
		{
			var combined = profile.GenericPool
				.Concat(profile.SystemSpecificPool)
				.OrderBy(entry => entry.Slug, StringComparer.Ordinal)
				.ToArray();
			if (!TryAddSelection(combined, random, slugCounts, usedNames, selections))
				break;
		}

		return selections;
	}

	private static bool TryAddSelection(
		IReadOnlyList<NavigationLandmarkPoolEntry> pool,
		StableRandom random,
		Dictionary<string, int> slugCounts,
		HashSet<string> usedNames,
		List<SelectedEntry> selections)
	{
		var ordered = pool.OrderBy(entry => entry.Slug, StringComparer.Ordinal).ToArray();
		var available = new List<(NavigationLandmarkPoolEntry Entry, string Name)>();
		foreach (var entry in ordered)
		{
			slugCounts.TryGetValue(entry.Slug, out var used);
			if (used >= entry.MaxPerMap)
				continue;

			foreach (var name in entry.Names)
			{
				if (usedNames.Contains(name))
					continue;

				available.Add((entry, name));
			}
		}

		if (available.Count == 0)
			return false;

		var totalWeight = available.Sum(item => item.Entry.Weight);
		var roll = random.NextDouble() * totalWeight;
		var cumulative = 0f;
		foreach (var item in available)
		{
			cumulative += item.Entry.Weight;
			if (roll > cumulative)
				continue;

			CommitSelection(item.Entry, item.Name, slugCounts, usedNames, selections);
			return true;
		}

		var fallback = available[^1];
		CommitSelection(fallback.Entry, fallback.Name, slugCounts, usedNames, selections);
		return true;

		static void CommitSelection(
			NavigationLandmarkPoolEntry entry,
			string name,
			Dictionary<string, int> slugCounts,
			HashSet<string> usedNames,
			List<SelectedEntry> selections)
		{
			slugCounts.TryGetValue(entry.Slug, out var used);
			slugCounts[entry.Slug] = used + 1;
			usedNames.Add(name);
			selections.Add(new SelectedEntry(entry, name));
		}
	}

	private static bool TryPlace(
		StarSystemBlueprint blueprint,
		int layoutAttempt,
		int landmarkIndex,
		SelectedEntry selection,
		IReadOnlyList<PointOfInterest> pois,
		IReadOnlyList<SpaceRoute> routes,
		NavigationLandmarkGenerationProfile profile,
		List<NavigationLandmark> placed,
		Dictionary<string, int> idOrdinals,
		out NavigationLandmark landmark)
	{
		var random = CreateRandom(
			blueprint.Seed,
			layoutAttempt,
			PlacementTag,
			landmarkIndex);
		var radius = selection.Entry.Radius;
		var candidates = new List<(Coord Position, double Score)>();

		for (var sample = 0; sample < profile.CandidateSampleCount; sample++)
		{
			var margin = radius + profile.EdgePadding;
			if (margin * 2 >= blueprint.Width || margin * 2 >= blueprint.Height)
			{
				landmark = default!;
				return false;
			}

			var x = margin + (int)(random.NextDouble() * (blueprint.Width - 2 * margin));
			var z = margin + (int)(random.NextDouble() * (blueprint.Height - 2 * margin));
			var position = new Coord(x, 0, z);

			if (!IsValidPosition(position, radius, blueprint, pois, routes, profile, placed))
				continue;

			var score = MinClearanceScore(position, radius, pois, routes, profile, placed);
			candidates.Add((position, score));
		}

		if (candidates.Count == 0)
		{
			landmark = default!;
			return false;
		}

		var maxScore = candidates.Max(candidate => candidate.Score);
		var threshold = maxScore * (1.0 - TopBandFraction);
		var band = candidates.Where(candidate => candidate.Score >= threshold).ToArray();
		var chosen = band[PickIndex(band.Length, random)];

		idOrdinals.TryGetValue(selection.Entry.Slug, out var ordinal);
		var id = $"landmark:{selection.Entry.Slug}:{ordinal:D2}";
		idOrdinals[selection.Entry.Slug] = ordinal + 1;
		var visualSeed = (int)StableSeedMixer.From(blueprint.Seed)
			.Add(layoutAttempt)
			.Add(landmarkIndex)
			.Add(selection.Entry.Slug)
			.Add("visual")
			.Value;

		landmark = new NavigationLandmark(
			id,
			selection.DisplayName,
			selection.Entry.Kind,
			chosen.Position,
			radius,
			visualSeed);
		return true;
	}

	private static int PickIndex(int count, StableRandom random) =>
		count <= 1 ? 0 : (int)(random.NextDouble() * count);

	private static bool IsValidPosition(
		Coord position,
		int radius,
		StarSystemBlueprint blueprint,
		IReadOnlyList<PointOfInterest> pois,
		IReadOnlyList<SpaceRoute> routes,
		NavigationLandmarkGenerationProfile profile,
		List<NavigationLandmark> placed)
	{
		if (!GridBounds.IsCircleWhollyInRectangle(position, radius, blueprint.Width, blueprint.Height))
			return false;

		foreach (var poi in pois)
		{
			var clearance = poi.RouteExclusionRadius + profile.PoiClearance + radius;
			var dx = position.X - poi.PlacedCenter.X;
			var dz = position.Z - poi.PlacedCenter.Z;
			if (dx * (long)dx + dz * (long)dz < (long)clearance * clearance)
				return false;
		}

		foreach (var route in routes)
		{
			var distance = RouteGeometry.PointToPolylineDistance(position, route.Centerline);
			var clearance = route.HalfWidth + profile.RouteClearance + radius;
			if (distance < clearance)
				return false;
		}

		foreach (var other in placed)
		{
			var separation = other.Radius + profile.LandmarkSeparation + radius;
			var dx = position.X - other.Position.X;
			var dz = position.Z - other.Position.Z;
			if (dx * (long)dx + dz * (long)dz < (long)separation * separation)
				return false;
		}

		return true;
	}

	private static double MinClearanceScore(
		Coord position,
		int radius,
		IReadOnlyList<PointOfInterest> pois,
		IReadOnlyList<SpaceRoute> routes,
		NavigationLandmarkGenerationProfile profile,
		List<NavigationLandmark> placed)
	{
		var minClearance = double.PositiveInfinity;

		foreach (var poi in pois)
		{
			var dx = position.X - poi.PlacedCenter.X;
			var dz = position.Z - poi.PlacedCenter.Z;
			var distance = System.Math.Sqrt(dx * dx + dz * dz);
			var clearance = distance - poi.RouteExclusionRadius - profile.PoiClearance - radius;
			minClearance = System.Math.Min(minClearance, clearance);
		}

		foreach (var route in routes)
		{
			var distance = RouteGeometry.PointToPolylineDistance(position, route.Centerline);
			var clearance = distance - route.HalfWidth - profile.RouteClearance - radius;
			minClearance = System.Math.Min(minClearance, clearance);
		}

		foreach (var other in placed)
		{
			var dx = position.X - other.Position.X;
			var dz = position.Z - other.Position.Z;
			var distance = System.Math.Sqrt(dx * dx + dz * dz);
			var clearance = distance - other.Radius - profile.LandmarkSeparation - radius;
			minClearance = System.Math.Min(minClearance, clearance);
		}

		return minClearance;
	}

	private static StableRandom CreateRandom(int seed, int layoutAttempt, string tag) =>
		new(StableSeedMixer.From(seed).Add(layoutAttempt).Add(tag).Value);

	private static StableRandom CreateRandom(int seed, int layoutAttempt, string tag, int index) =>
		new(StableSeedMixer.From(seed).Add(layoutAttempt).Add(tag).Add(index).Value);
}
