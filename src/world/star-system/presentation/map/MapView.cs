using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.Math;
using GrimSpace.Math.Camera;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.World.StarSystem.Presentation.Map;

public partial class MapView : Node3D
{
	private const int FineEvery = 64;
	private const int MajorEvery = 256;
	private const float MinorMarkSize = 0.048f;
	private const float MajorMarkSize = 0.078f;
	private const float MinorAlpha = 0.34f;
	private const float MajorAlpha = 0.56f;
	private const float GridHaloScale = 1.35f;
	private const float GridHaloAlpha = 0.12f;
	private const int FootprintSegments = 48;
	private const float IndicatorClearancePadding = 0.15f;
	private const string TradeHubModelPath = "res://assets/models/spaceobjects/sci-fi_space_station_2.glb";
	private const string StorageModelPath = "res://assets/models/spaceobjects/freeport_space_station1.glb";
	private const string AdminStationModelPath = "res://assets/models/spaceobjects/gangut_space_hub.glb";
	private const string WormholeModelPath = "res://assets/models/spaceobjects/black_hole_station_daily_draft_35.glb";

	private static readonly Color GridMinor = new(0.28f, 0.78f, 0.88f, MinorAlpha);
	private static readonly Color GridMajor = new(0.36f, 0.88f, 0.96f, MajorAlpha);
	private static readonly Color HoverAccent = new(0.41f, 0.69f, 0.76f, 0.28f);
	private static readonly Color DockMarkerColor = new(0.45f, 0.72f, 0.78f, 0.85f);

	private readonly Dictionary<string, MeshInstance3D> _footprints = new();
	private readonly Dictionary<string, Node3D> _markers = new();
	private readonly Dictionary<string, MeshInstance3D> _hoverRings = new();

	private string? _hoveredId;
	private Node3D? _minorGridRoot;
	private MapAtmosphereSettings _atmosphere = MapAtmosphereSettings.Default;
	private IReadOnlyList<PointOfInterest> _pois = [];
	private IReadOnlyList<Dock> _docks = [];
	private IReadOnlyDictionary<string, Dock> _docksByPoiId = new Dictionary<string, Dock>();
	private IReadOnlyDictionary<string, string> _dockDisplayNames = new Dictionary<string, string>();

	private const int DockPickRadius = 12;

	public sealed record DockHoverInfo(string DockId, string PoiId, string DisplayName);

	public Vector3 GetPoiWorldPosition(string poiId, int width, int height)
	{
		if (_markers.TryGetValue(poiId, out var marker))
			return marker.GlobalPosition;

		var poi = _pois.First(p => p.Id == poiId);
		return MapMapping.ToWorld(poi.PlacedCenter, width, height);
	}

	public float GetIndicatorClearance(string objectId, StarMap world)
	{
		var poi = world.PointsOfInterest.FirstOrDefault(candidate => candidate.Id == objectId);
		if (poi is not null)
			return MapMapping.ToWorldRadius(poi.Radius) + IndicatorClearancePadding;

		var landmark = world.NavigationLandmarks.FirstOrDefault(candidate => candidate.Id == objectId);
		if (landmark is not null)
			return MapMapping.ToWorldRadius(landmark.Radius) + IndicatorClearancePadding;

		if (world.FleetRegistry.TryGet(objectId, out _))
			return 0.12f;

		return 0f;
	}

	public Vector3 GetDockWorldPosition(string poiId, int width, int height)
	{
		if (_docksByPoiId.TryGetValue(poiId, out var dock))
			return MapMapping.ToWorld(dock.Position, width, height);

		return GetPoiWorldPosition(poiId, width, height);
	}

	public OrbitPose ResolveFacadePose(PointOfInterest poi, int width, int height)
	{
		var def = poi.Facade;
		return new OrbitPose
		{
			Pivot = MapMapping.ToWorld(poi.PlacedCenter, width, height)
				+ new Vector3(def.PivotOffsetX, def.PivotOffsetY, def.PivotOffsetZ),
			Yaw = Mathf.DegToRad(def.YawDegrees),
			Pitch = Mathf.DegToRad(def.PitchDegrees),
			Distance = def.Distance,
		};
	}

	public Vector3 ResolveFacilityAnchorWorldPosition(
		PointOfInterest poi,
		EPresentationAnchor anchor,
		int width,
		int height)
	{
		var basePosition = GetPoiWorldPosition(poi.Id, width, height);
		return basePosition + FacilityAnchorOffsets.Resolve(anchor, poi.Facade.Layout);
	}

	public void ConfigureAtmosphere(MapAtmosphereSettings settings) =>
		_atmosphere = settings;

	public void Build(StarMap world)
	{
		foreach (var child in GetChildren().ToArray())
		{
			RemoveChild(child);
			child.Free();
		}

		_footprints.Clear();
		_markers.Clear();
		_hoverRings.Clear();
		_hoveredId = null;
		_minorGridRoot = null;
		_pois = world.PointsOfInterest;
		_docks = world.DocksById.Values.ToList();
		_docksByPoiId = world.DocksByPoiId;
		_dockDisplayNames = world.PointsOfInterest.ToDictionary(
			poi => poi.Id,
			poi => poi.DisplayName,
			StringComparer.Ordinal);

		AddChild(BuildReferenceGrid(world.Width, world.Height));

		var planetVariants = MapCelestialVisuals.AssignPlanetVariants(
			world.Seed,
			world.PointsOfInterest
				.Where(poi => poi is Refinery
					|| poi is AdministrativeCore { PhysicalForm: EPoiPhysicalForm.Planet })
				.Select(poi => poi.Id));
		foreach (var poi in world.PointsOfInterest)
		{
			var footprint = BuildPoiFootprint(poi, world.Width, world.Height);
			footprint.Visible = false;
			_footprints[poi.Id] = footprint;
			AddChild(footprint);

			var marker = BuildPoiMarker(poi, world.Seed, world.Width, world.Height, planetVariants);
			_markers[poi.Id] = marker;
			AddChild(marker);
		}

		foreach (var dock in _docks)
			AddChild(BuildDockMarker(dock, world.Width, world.Height));
	}

	public void SetHovered(string? poiId)
	{
		if (_hoveredId == poiId)
			return;

		if (_hoveredId is not null)
		{
			if (_footprints.TryGetValue(_hoveredId, out var prevFp))
				prevFp.Visible = false;
			if (_hoverRings.TryGetValue(_hoveredId, out var prevRing))
				prevRing.Visible = false;
			if (_markers.TryGetValue(_hoveredId, out var prevMarker))
				prevMarker.Scale = Vector3.One;
		}

		_hoveredId = poiId;

		if (poiId is null)
			return;

		if (_footprints.TryGetValue(poiId, out var fp))
			fp.Visible = true;
		if (_hoverRings.TryGetValue(poiId, out var ring))
			ring.Visible = true;
		if (_markers.TryGetValue(poiId, out var marker))
			marker.Scale = Vector3.One * 1.04f;
	}

	public void SetCameraDistance(float distance)
	{
		if (_minorGridRoot is not null)
			_minorGridRoot.Visible = distance < 48f;
	}

	public string? PoiAt(Coord point)
	{
		string? bestId = null;
		var bestDistance = long.MaxValue;

		foreach (var poi in _pois)
		{
			if (!ContainsPoint(poi, point))
				continue;

			var dx = point.X - poi.PlacedCenter.X;
			var dz = point.Z - poi.PlacedCenter.Z;
			var distance = (long)dx * dx + (long)dz * dz;
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestId = poi.Id;
		}

		return bestId;
	}

	public DockHoverInfo? DockAt(Coord point)
	{
		DockHoverInfo? best = null;
		var bestDistance = long.MaxValue;

		foreach (var dock in _docks)
		{
			var dx = point.X - dock.Position.X;
			var dz = point.Z - dock.Position.Z;
			var distance = (long)dx * dx + (long)dz * dz;
			if (distance > (long)DockPickRadius * DockPickRadius || distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = new DockHoverInfo(
				dock.Id,
				dock.PoiId,
				_dockDisplayNames.GetValueOrDefault(dock.PoiId, dock.PoiId));
		}

		return best;
	}

	/// <summary>
	/// Snaps a picked grid cell to the nearest dock when clicking a dock marker or POI footprint.
	/// Docking requires an exact dock coordinate at journey end.
	/// </summary>
	public Coord ResolveMoveDestination(Coord picked)
	{
		if (DockAt(picked) is { } dockHover
			&& _docksByPoiId.TryGetValue(dockHover.PoiId, out var dock))
		{
			return dock.Position;
		}

		if (PoiAt(picked) is { } poiId
			&& _docksByPoiId.TryGetValue(poiId, out var poiDock))
		{
			return poiDock.Position;
		}

		return picked;
	}

	private static Node3D BuildDockMarker(Dock dock, int width, int height)
	{
		var root = new Node3D { Name = $"Dock_{dock.Id}" };
		root.AddChild(new MeshInstance3D
		{
			Name = "Marker",
			Position = MapMapping.ToWorld(dock.Position, width, height),
			Mesh = new SphereMesh { Radius = 0.10f, Height = 0.20f },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = DockMarkerColor,
				EmissionEnabled = true,
				Emission = DockMarkerColor,
				EmissionEnergyMultiplier = 0.45f,
			},
		});
		return root;
	}

	private static bool ContainsPoint(PointOfInterest poi, Coord point)
	{
		var dx = point.X - poi.PlacedCenter.X;
		var dz = point.Z - poi.PlacedCenter.Z;
		return (long)dx * dx + (long)dz * dz <= (long)poi.Radius * poi.Radius;
	}

	private Node3D BuildReferenceGrid(int width, int height)
	{
		var root = new Node3D { Name = "Grid" };
		_minorGridRoot = BuildDotMesh(
			"Minor",
			width,
			height,
			GridMinor,
			FineEvery,
			MinorMarkSize,
			major: false,
			withHalo: true);
		root.AddChild(_minorGridRoot);
		root.AddChild(BuildDotMesh(
			"Major",
			width,
			height,
			GridMajor,
			FineEvery,
			MajorMarkSize,
			major: true,
			withHalo: false));
		return root;
	}

	private static Node3D BuildDotMesh(
		string name,
		int width,
		int height,
		Color color,
		int step,
		float markSize,
		bool major,
		bool withHalo)
	{
		var instances = new List<(Vector3 Position, Color InstanceColor)>();

		for (var x = 0; x < width; x += step)
		{
			for (var z = 0; z < height; z += step)
			{
				var onMajor = x % MajorEvery == 0 && z % MajorEvery == 0;
				if (major != onMajor)
					continue;

				var position = MapMapping.ToWorld(new Coord(x, 0, z), width, height);
				instances.Add((position, color));
			}
		}

		var root = new Node3D { Name = name };
		var radius = markSize * 0.5f;
		root.AddChild(BuildSphereMultiMesh($"{name}Core", instances, radius, color));
		if (withHalo)
		{
			var haloRadius = radius * GridHaloScale;
			var haloColor = color with { A = GridHaloAlpha };
			root.AddChild(BuildSphereMultiMesh($"{name}Halo", instances, haloRadius, haloColor));
		}

		return root;
	}

	private static MultiMeshInstance3D BuildSphereMultiMesh(
		string name,
		IReadOnlyList<(Vector3 Position, Color InstanceColor)> instances,
		float radius,
		Color materialTint)
	{
		var sphereMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
			AlbedoColor = materialTint,
			VertexColorUseAsAlbedo = true,
			EmissionEnabled = true,
			Emission = materialTint,
			EmissionEnergyMultiplier = 0.35f,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};
		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			InstanceCount = instances.Count,
			Mesh = new SphereMesh
			{
				Radius = radius,
				Height = radius * 2f,
				Material = sphereMaterial,
			},
		};

		for (var i = 0; i < instances.Count; i++)
		{
			var (position, instanceColor) = instances[i];
			position.Y = radius;
			multiMesh.SetInstanceTransform(i, new Transform3D(Basis.Identity, position));
			multiMesh.SetInstanceColor(i, instanceColor);
		}

		return new MultiMeshInstance3D
		{
			Name = name,
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
	}

	private static MeshInstance3D BuildPoiFootprint(PointOfInterest poi, int width, int height)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(HoverAccent);

		var center = MapMapping.ToWorld(poi.PlacedCenter, width, height);
		var worldRadius = MapMapping.ToWorldRadius(poi.Radius);
		AddCircleOutline(mesh, center, worldRadius);

		mesh.SurfaceEnd();

		return new MeshInstance3D
		{
			Name = $"Footprint_{poi.Id}",
			Mesh = mesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				VertexColorUseAsAlbedo = true,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			},
		};
	}

	private Node3D BuildPoiMarker(
		PointOfInterest poi,
		int seed,
		int width,
		int height,
		IReadOnlyDictionary<string, int> planetVariants)
	{
		var root = new Node3D
		{
			Name = $"Marker_{poi.Id}",
			Position = MapMapping.ToWorld(poi.PlacedCenter, width, height),
		};

		float ringRadius;
		switch (poi)
		{
			case Star star:
				MapCelestialVisuals.AddStar(root, seed, star.Radius, _atmosphere);
				ringRadius = MapMapping.ToWorldRadius(star.Radius) * 1.25f;
				break;
			case Refinery:
				MapCelestialVisuals.AddPlanet(root, planetVariants[poi.Id], _atmosphere);
				ringRadius = 0.95f;
				break;
			case OreMine:
				AddAsteroidField(root, seed, poi);
				ringRadius = 1.05f;
				break;
			case Wormhole:
				AddImportedPoiModel(root, WormholeModelPath, "WormholeStation", 1.4f, Basis.Identity);
				ringRadius = 0.88f;
				break;
			case StorageFacility:
				AddImportedPoiModel(root, StorageModelPath, "StorageStation", 1.3225f, Basis.Identity);
				ringRadius = 0.75f;
				break;
			case TradeHub:
				AddImportedPoiModel(
					root,
					TradeHubModelPath,
					"TradeHubStation",
					1.25f,
					Basis.Identity);
				ringRadius = 0.82f;
				break;
			case AdministrativeCore admin:
				if (admin.PhysicalForm == EPoiPhysicalForm.Planet)
				{
					MapCelestialVisuals.AddPlanet(root, planetVariants[poi.Id], _atmosphere);
					ringRadius = 1.0f;
				}
				else
				{
					AddImportedPoiModel(root, AdminStationModelPath, "AdminStation", 1.4f, Basis.Identity);
					ringRadius = 0.95f;
				}
				break;
			default:
				throw new InvalidOperationException($"Unsupported POI type '{poi.GetType().Name}'.");
		}

		var ring = new MeshInstance3D
		{
			Name = "HoverRing",
			Position = Vector3.Zero,
			Mesh = new TorusMesh { InnerRadius = ringRadius - 0.025f, OuterRadius = ringRadius },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoColor = HoverAccent with { A = 0.40f },
				EmissionEnabled = true,
				Emission = HoverAccent,
				EmissionEnergyMultiplier = 0.35f,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
			Visible = false,
		};
		_hoverRings[poi.Id] = ring;
		root.AddChild(ring);

		return root;
	}

	private static void AddAsteroidField(Node3D root, int seed, PointOfInterest poi)
	{
		var random = new StableRandom(StableSeedMixer.From(seed).Add(poi.Id).Value);
		var worldRadius = poi.Radius * MapMapping.WorldUnitsPerPoint;
		var mainCount = 3 + (int)(random.NextDouble() * 2);
		var supportCount = 14 + (int)(random.NextDouble() * 7);
		var count = mainCount + supportCount + 34 + (int)(random.NextDouble() * 13);
		var orientation = random.NextDouble() * System.Math.Tau;
		var copper = new Color(0.98f, 0.37f, 0.15f);
		var weatheredCopper = new Color(0.72f, 0.29f, 0.16f);

		for (var i = 0; i < count; i++)
		{
			var rng = CreateGodotRng(seed, poi.Id, i);
			var mainMass = i < mainCount;
			var supportRock = i < mainCount + supportCount;
			var angle = mainMass
				? orientation + i * System.Math.Tau / mainCount + (random.NextDouble() - 0.5) * 0.25
				: random.NextDouble() * System.Math.Tau;
			var distance = mainMass
				? worldRadius * (0.32 + random.NextDouble() * 0.22)
				: worldRadius * ((supportRock ? 0.14 : 0.18)
					+ System.Math.Sqrt(random.NextDouble()) * (supportRock ? 0.68 : 0.75));
			var lift = (random.NextDouble() - 0.5) * worldRadius * 0.18;
			var diameter = worldRadius * (mainMass
				? 0.38f + (float)random.NextDouble() * 0.16f
				: supportRock
					? 0.12f + (float)random.NextDouble() * 0.11f
					: 0.065f + (float)random.NextDouble() * 0.045f);
			var rock = NavigationLandmarkRockLibrary.CreateRock(rng, diameter, mainMass);
			rock.Name = $"Rock_{i}";
			rock.Position = new Vector3(
				(float)(System.Math.Cos(angle) * distance),
				(float)lift,
				(float)(System.Math.Sin(angle) * distance));
			rock.Rotation = new Vector3(
				(float)(random.NextDouble() * System.Math.Tau),
				(float)(random.NextDouble() * System.Math.Tau),
				(float)(random.NextDouble() * System.Math.Tau));
			NavigationLandmarkRockLibrary.TintMeshes(
				rock,
				copper.Lerp(weatheredCopper, rng.Randf() * 0.55f),
				emissionStrength: 0.05f);
			root.AddChild(rock);
		}
	}

	private static RandomNumberGenerator CreateGodotRng(int seed, string poiId, int index)
	{
		var godotRng = new RandomNumberGenerator
		{
			Seed = StableSeedMixer.From(seed).Add(poiId).Add(index).Value,
		};
		return godotRng;
	}

	private static void AddImportedPoiModel(
		Node3D root,
		string path,
		string name,
		float targetDiameter,
		Basis rotation)
	{
		var scene = GD.Load<PackedScene>(path)
			?? throw new InvalidOperationException($"Could not load POI model '{path}'.");
		var model = scene.Instantiate<Node3D>();
		var parts = model.FindChildren("*", "MeshInstance3D", true, false)
			.OfType<MeshInstance3D>().ToArray();
		if (parts.Length == 0 || parts.Any(part => part.Mesh is null))
		{
			model.Free();
			throw new InvalidOperationException($"POI model '{path}' has missing meshes.");
		}

		var bounds = default(Aabb);
		for (var i = 0; i < parts.Length; i++)
		{
			var transform = Transform3D.Identity;
			for (Node? node = parts[i]; node is not null && node != model; node = node.GetParent())
			{
				if (node is Node3D spatial)
					transform = spatial.Transform * transform;
			}

			var partBounds = transform * parts[i].Mesh.GetAabb();
			bounds = i == 0 ? partBounds : bounds.Merge(partBounds);
		}

		var rotatedBounds = new Transform3D(rotation, Vector3.Zero) * bounds;
		var diameter = Mathf.Max(rotatedBounds.Size.X, rotatedBounds.Size.Z);
		if (diameter <= 0f)
		{
			model.Free();
			throw new InvalidOperationException($"POI model '{path}' has no horizontal extent.");
		}

		var scale = targetDiameter / diameter;
		model.Name = name;
		model.Basis = rotation.ScaledLocal(Vector3.One * scale);
		model.Position = -(rotation * (bounds.GetCenter() * scale));
		root.AddChild(model);
	}

	private static void AddCircleOutline(ImmediateMesh mesh, Vector3 center, float radius)
	{
		for (var i = 0; i < FootprintSegments; i++)
		{
			var a = i * Mathf.Tau / FootprintSegments;
			var b = (i + 1) * Mathf.Tau / FootprintSegments;
			mesh.SurfaceAddVertex(center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
			mesh.SurfaceAddVertex(center + new Vector3(Mathf.Cos(b) * radius, 0f, Mathf.Sin(b) * radius));
		}
	}

	private static class FacilityAnchorOffsets
	{
		private static readonly Vector3 NeutralOffset = new(0f, 0.35f, 0f);

		private static readonly (EPresentationAnchor Anchor, EFacadeLayout Layout, Vector3 Offset)[] Entries =
		[
			(EPresentationAnchor.Management, EFacadeLayout.Planet, new Vector3(0.35f, 0.55f, 0.12f)),
			(EPresentationAnchor.Management, EFacadeLayout.Station, new Vector3(0.22f, 0.42f, 0.18f)),
			(EPresentationAnchor.Management, EFacadeLayout.Default, new Vector3(0.28f, 0.38f, 0f)),
			(EPresentationAnchor.Dockyard, EFacadeLayout.Station, new Vector3(-0.18f, 0.38f, 0.14f)),
			(EPresentationAnchor.Dockyard, EFacadeLayout.Default, new Vector3(-0.12f, 0.36f, 0f)),
			(EPresentationAnchor.Warehouse, EFacadeLayout.Station, new Vector3(0.2f, 0.4f, -0.1f)),
			(EPresentationAnchor.Warehouse, EFacadeLayout.Default, new Vector3(0.15f, 0.38f, 0f)),
			(EPresentationAnchor.Refinery, EFacadeLayout.Station, new Vector3(-0.2f, 0.42f, 0.08f)),
			(EPresentationAnchor.Refinery, EFacadeLayout.Default, new Vector3(-0.15f, 0.38f, 0f)),
			(EPresentationAnchor.Mine, EFacadeLayout.Station, new Vector3(-0.14f, 0.4f, 0.1f)),
			(EPresentationAnchor.Mine, EFacadeLayout.Default, new Vector3(-0.1f, 0.36f, 0f)),
			(EPresentationAnchor.Travel, EFacadeLayout.Station, new Vector3(0.1f, 0.44f, 0.1f)),
			(EPresentationAnchor.Travel, EFacadeLayout.Default, new Vector3(0.08f, 0.4f, 0f)),
			(EPresentationAnchor.Market, EFacadeLayout.Station, new Vector3(0.16f, 0.36f, -0.12f)),
			(EPresentationAnchor.Market, EFacadeLayout.Default, new Vector3(0.1f, 0.34f, 0f)),
		];

		public static Vector3 Resolve(EPresentationAnchor anchor, EFacadeLayout layout)
		{
			foreach (var (entryAnchor, entryLayout, offset) in Entries)
			{
				if (entryAnchor == anchor && entryLayout == layout)
					return offset;
			}

			GD.PushWarning(
				$"MapView: no facility anchor offset for {anchor} on layout {layout}; using neutral offset.");
			return NeutralOffset;
		}
	}
}
