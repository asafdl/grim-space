using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapView : Node3D
{
	private const int FineEvery = 32;
	private const int MajorEvery = 256;
	private const float MinorAlpha = 0.015f;
	private const float MajorAlpha = 0.055f;
	private const float BoundaryAlpha = 0.12f;
	private const int FootprintSegments = 48;

	private static readonly Color GridMinor = new(0.15f, 0.22f, 0.30f, MinorAlpha);
	private static readonly Color GridMajor = new(0.22f, 0.32f, 0.42f, MajorAlpha);
	private static readonly Color GridBoundary = new(0.32f, 0.44f, 0.54f, BoundaryAlpha);
	private static readonly Color HoverAccent = new(0.41f, 0.69f, 0.76f, 0.28f);
	private static readonly Color StarColor = new(0.89f, 0.66f, 0.33f);
	private static readonly Color StationBeacon = new(0.45f, 0.72f, 0.78f);
	private static readonly Color DockMarkerColor = new(0.45f, 0.72f, 0.78f, 0.85f);

	private static readonly Color[] PlanetPalette =
	[
		new(0.20f, 0.26f, 0.34f),
		new(0.16f, 0.26f, 0.28f),
		new(0.32f, 0.20f, 0.16f),
	];

	private readonly Dictionary<string, MeshInstance3D> _footprints = new();
	private readonly Dictionary<string, Node3D> _markers = new();
	private readonly Dictionary<string, MeshInstance3D> _hoverRings = new();

	private string? _hoveredId;
	private MeshInstance3D? _minorGrid;
	private IReadOnlyList<PointOfInterest> _pois = [];
	private IReadOnlyList<Dock> _docks = [];
	private IReadOnlyDictionary<string, string> _dockDisplayNames = new Dictionary<string, string>();

	private const int DockPickRadius = 12;

	public sealed record DockHoverInfo(string DockId, string PoiId, string DisplayName);

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
		_minorGrid = null;
		_pois = world.PointsOfInterest;
		_docks = world.DocksById.Values.ToList();
		_dockDisplayNames = world.PointsOfInterest.ToDictionary(
			poi => poi.Id,
			poi => poi.DisplayName,
			StringComparer.Ordinal);

		AddChild(BuildReferenceGrid(world.Width, world.Height));

		var planetIndex = 0;
		foreach (var poi in world.PointsOfInterest)
		{
			var footprint = BuildPoiFootprint(poi, world.Width, world.Height);
			footprint.Visible = false;
			_footprints[poi.Id] = footprint;
			AddChild(footprint);

			var marker = BuildPoiMarker(poi, world.Width, world.Height, ref planetIndex);
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
		if (_minorGrid is not null)
			_minorGrid.Visible = distance < 48f;
	}

	public string? PoiAt(Coord point)
	{
		string? bestId = null;
		var bestDistance = long.MaxValue;

		foreach (var poi in _pois)
		{
			if (!ContainsPoint(poi, point))
				continue;

			var dx = point.X - poi.Center.X;
			var dz = point.Z - poi.Center.Z;
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
		var dx = point.X - poi.Center.X;
		var dz = point.Z - poi.Center.Z;
		return (long)dx * dx + (long)dz * dz <= (long)poi.Radius * poi.Radius;
	}

	private Node3D BuildReferenceGrid(int width, int height)
	{
		var root = new Node3D { Name = "Grid" };
		var origin = MapMapping.GridOrigin(width, height);
		var extent = width * MapMapping.WorldUnitsPerPoint;

		_minorGrid = BuildAxisLines("Minor", width, height, origin, extent, GridMinor, major: false);
		root.AddChild(_minorGrid);
		root.AddChild(BuildAxisLines("Major", width, height, origin, extent, GridMajor, major: true));
		root.AddChild(BuildBoundary(origin, extent));
		return root;
	}

	private static MeshInstance3D BuildAxisLines(
		string name,
		int width,
		int height,
		Vector3 origin,
		float extent,
		Color color,
		bool major)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(color);

		for (var x = FineEvery; x < width; x += FineEvery)
		{
			if ((x % MajorEvery == 0) != major)
				continue;

			var wx = MapMapping.ToWorld(new Coord(x, 0, 0), width, height).X;
			mesh.SurfaceAddVertex(new Vector3(wx, 0f, origin.Z));
			mesh.SurfaceAddVertex(new Vector3(wx, 0f, origin.Z + extent));
		}

		for (var z = FineEvery; z < height; z += FineEvery)
		{
			if ((z % MajorEvery == 0) != major)
				continue;

			var wz = MapMapping.ToWorld(new Coord(0, 0, z), width, height).Z;
			mesh.SurfaceAddVertex(new Vector3(origin.X, 0f, wz));
			mesh.SurfaceAddVertex(new Vector3(origin.X + extent, 0f, wz));
		}

		mesh.SurfaceEnd();
		return LineMesh(name, mesh);
	}

	private static MeshInstance3D BuildBoundary(Vector3 origin, float extent)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(GridBoundary);

		var min = new Vector3(origin.X, 0f, origin.Z);
		var max = new Vector3(origin.X + extent, 0f, origin.Z + extent);
		AddQuadOutline(mesh, min, max);
		mesh.SurfaceEnd();
		return LineMesh("Boundary", mesh);
	}

	private static MeshInstance3D LineMesh(string name, ImmediateMesh mesh) =>
		new()
		{
			Name = name,
			Mesh = mesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				VertexColorUseAsAlbedo = true,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			},
		};

	private static MeshInstance3D BuildPoiFootprint(PointOfInterest poi, int width, int height)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(HoverAccent);

		var center = MapMapping.ToWorld(poi.Center, width, height);
		var worldRadius = poi.Radius * MapMapping.WorldUnitsPerPoint;
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

	private Node3D BuildPoiMarker(PointOfInterest poi, int width, int height, ref int planetIndex)
	{
		var root = new Node3D
		{
			Name = $"Marker_{poi.Id}",
			Position = MapMapping.ToWorld(poi.Center, width, height),
		};

		switch (poi.Kind)
		{
			case EPointOfInterestKind.Star:
				AddStar(root);
				break;
			case EPointOfInterestKind.Planet:
				AddPlanet(root, PlanetPalette[planetIndex++ % PlanetPalette.Length]);
				break;
			default:
				AddStation(root);
				break;
		}

		var ringRadius = poi.Kind switch
		{
			EPointOfInterestKind.Star => 1.25f,
			EPointOfInterestKind.Planet => 0.95f,
			_ => 0.75f,
		};
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

	private static void AddStar(Node3D root)
	{
		root.AddChild(new MeshInstance3D
		{
			Name = "Core",
			Position = Vector3.Zero,
			Mesh = new SphereMesh { Radius = 1.0f, Height = 2.0f },
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = StarColor,
				EmissionEnabled = true,
				Emission = StarColor,
				EmissionEnergyMultiplier = 1.35f,
			},
		});

		root.AddChild(new MeshInstance3D
		{
			Name = "Halo",
			Position = Vector3.Zero,
			Mesh = new SphereMesh { Radius = 1.45f, Height = 2.9f },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoColor = new Color(0.89f, 0.66f, 0.33f, 0.07f),
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
		});
	}

	private static void AddPlanet(Node3D root, Color surface)
	{
		root.AddChild(new MeshInstance3D
		{
			Name = "Body",
			Position = Vector3.Zero,
			Mesh = new SphereMesh { Radius = 0.55f, Height = 1.1f },
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = surface,
				Roughness = 0.88f,
			},
		});

		root.AddChild(new MeshInstance3D
		{
			Name = "Orbit",
			Position = Vector3.Zero,
			RotationDegrees = new Vector3(12f, 18f, 0f),
			Mesh = new TorusMesh { InnerRadius = 0.78f, OuterRadius = 0.81f },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoColor = new Color(0.50f, 0.58f, 0.68f, 0.18f),
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
		});
	}

	private static void AddStation(Node3D root)
	{
		var steel = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.20f, 0.22f, 0.26f),
			Metallic = 0.55f,
			Roughness = 0.45f,
		};

		root.AddChild(new MeshInstance3D
		{
			Name = "Hub",
			Position = Vector3.Zero,
			Mesh = new SphereMesh { Radius = 0.20f, Height = 0.40f },
			MaterialOverride = steel,
		});

		root.AddChild(new MeshInstance3D
		{
			Name = "Ring",
			Position = Vector3.Zero,
			RotationDegrees = new Vector3(90f, 0f, 0f),
			Mesh = new TorusMesh { InnerRadius = 0.36f, OuterRadius = 0.44f },
			MaterialOverride = steel,
		});

		root.AddChild(new MeshInstance3D
		{
			Name = "ArmA",
			Position = Vector3.Zero,
			Mesh = new BoxMesh { Size = new Vector3(0.95f, 0.06f, 0.12f) },
			MaterialOverride = steel,
		});

		root.AddChild(new MeshInstance3D
		{
			Name = "ArmB",
			Position = Vector3.Zero,
			Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.06f, 0.95f) },
			MaterialOverride = steel,
		});

		root.AddChild(new MeshInstance3D
		{
			Name = "Beacon",
			Position = new Vector3(0f, 0.28f, 0f),
			Mesh = new SphereMesh { Radius = 0.055f, Height = 0.11f },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = StationBeacon,
				EmissionEnabled = true,
				Emission = StationBeacon,
				EmissionEnergyMultiplier = 0.9f,
			},
		});
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

	private static void AddQuadOutline(ImmediateMesh mesh, Vector3 min, Vector3 max)
	{
		var a = new Vector3(min.X, min.Y, min.Z);
		var b = new Vector3(max.X, min.Y, min.Z);
		var c = new Vector3(max.X, min.Y, max.Z);
		var d = new Vector3(min.X, min.Y, max.Z);
		mesh.SurfaceAddVertex(a);
		mesh.SurfaceAddVertex(b);
		mesh.SurfaceAddVertex(b);
		mesh.SurfaceAddVertex(c);
		mesh.SurfaceAddVertex(c);
		mesh.SurfaceAddVertex(d);
		mesh.SurfaceAddVertex(d);
		mesh.SurfaceAddVertex(a);
	}
}
