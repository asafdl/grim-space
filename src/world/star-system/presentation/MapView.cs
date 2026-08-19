using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapView : Node3D
{
	private const int MajorEvery = 8;
	private const float MinorAlpha = 0.015f;
	private const float MajorAlpha = 0.055f;
	private const float BoundaryAlpha = 0.12f;

	private static readonly Color GridMinor = new(0.15f, 0.22f, 0.30f, MinorAlpha);
	private static readonly Color GridMajor = new(0.22f, 0.32f, 0.42f, MajorAlpha);
	private static readonly Color GridBoundary = new(0.32f, 0.44f, 0.54f, BoundaryAlpha);
	private static readonly Color HoverAccent = new(0.41f, 0.69f, 0.76f, 0.28f);
	private static readonly Color StarColor = new(0.89f, 0.66f, 0.33f);
	private static readonly Color StationBeacon = new(0.45f, 0.72f, 0.78f);

	private static readonly Color[] PlanetPalette =
	[
		new(0.20f, 0.26f, 0.34f),
		new(0.16f, 0.26f, 0.28f),
		new(0.32f, 0.20f, 0.16f),
	];

	private readonly Dictionary<Coord, string> _cellOwners = new();
	private readonly Dictionary<string, MeshInstance3D> _footprints = new();
	private readonly Dictionary<string, Node3D> _markers = new();
	private readonly Dictionary<string, MeshInstance3D> _hoverRings = new();

	private string? _hoveredId;
	private MeshInstance3D? _minorGrid;

	public void Build(StarMap world)
	{
		foreach (var child in GetChildren().ToArray())
		{
			RemoveChild(child);
			child.Free();
		}

		_cellOwners.Clear();
		_footprints.Clear();
		_markers.Clear();
		_hoverRings.Clear();
		_hoveredId = null;
		_minorGrid = null;

		AddChild(BuildGrid(world.Width, world.Height));

		var planetIndex = 0;
		foreach (var poi in world.PointsOfInterest)
		{
			foreach (var cell in poi.Cells)
				_cellOwners[cell] = poi.Id;

			var footprint = BuildPoiFootprint(poi, world.Width, world.Height);
			footprint.Visible = false;
			_footprints[poi.Id] = footprint;
			AddChild(footprint);

			var marker = BuildPoiMarker(poi, world.Width, world.Height, ref planetIndex);
			_markers[poi.Id] = marker;
			AddChild(marker);
		}
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

	public string? OwnerOf(Coord cell) =>
		_cellOwners.GetValueOrDefault(cell);

	private Node3D BuildGrid(int width, int height)
	{
		var root = new Node3D { Name = "Grid" };
		var origin = MapMapping.GridOrigin(width, height);
		var size = MapMapping.CellSize;

		_minorGrid = BuildAxisLines("Minor", width, height, origin, size, GridMinor, major: false);
		root.AddChild(_minorGrid);
		root.AddChild(BuildAxisLines("Major", width, height, origin, size, GridMajor, major: true));
		root.AddChild(BuildBoundary(width, height, origin, size));
		return root;
	}

	private static MeshInstance3D BuildAxisLines(
		string name,
		int width,
		int height,
		Vector3 origin,
		float size,
		Color color,
		bool major)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(color);

		for (var x = 1; x < width; x++)
		{
			if ((x % MajorEvery == 0) != major)
				continue;
			var wx = origin.X + x * size;
			mesh.SurfaceAddVertex(new Vector3(wx, 0f, origin.Z));
			mesh.SurfaceAddVertex(new Vector3(wx, 0f, origin.Z + height * size));
		}

		for (var z = 1; z < height; z++)
		{
			if ((z % MajorEvery == 0) != major)
				continue;
			var wz = origin.Z + z * size;
			mesh.SurfaceAddVertex(new Vector3(origin.X, 0f, wz));
			mesh.SurfaceAddVertex(new Vector3(origin.X + width * size, 0f, wz));
		}

		mesh.SurfaceEnd();
		return LineMesh(name, mesh);
	}

	private static MeshInstance3D BuildBoundary(int width, int height, Vector3 origin, float size)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(GridBoundary);

		var min = new Vector3(origin.X, 0f, origin.Z);
		var max = new Vector3(origin.X + width * size, 0f, origin.Z + height * size);
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

		foreach (var edge in ExteriorEdges(poi.Cells))
		{
			mesh.SurfaceAddVertex(CornerToWorld(edge.A, width, height));
			mesh.SurfaceAddVertex(CornerToWorld(edge.B, width, height));
		}

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
		var type = TypeSlug(poi.Id);
		var centroid = Centroid(poi.Cells);
		// Marker centre sits on the navigational plane (Y = 0); meshes extend ±radius.
		var root = new Node3D
		{
			Name = $"Marker_{poi.Id}",
			Position = MapMapping.ToWorld(centroid, width, height),
		};

		switch (type)
		{
			case "star":
				AddStar(root);
				break;
			case "planet":
				AddPlanet(root, PlanetPalette[planetIndex++ % PlanetPalette.Length]);
				break;
			default:
				AddStation(root);
				break;
		}

		var ringRadius = type switch
		{
			"star" => 1.25f,
			"planet" => 0.95f,
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

		// Slightly tipped so the ring reads in perspective while still coplanar-ish with the ecliptic.
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

	private static List<(Vector2I A, Vector2I B)> ExteriorEdges(IReadOnlySet<Coord> cells)
	{
		var occupied = cells.Select(c => (c.X, c.Z)).ToHashSet();
		var edges = new HashSet<(Vector2I A, Vector2I B)>();

		foreach (var (x, z) in occupied)
		{
			if (!occupied.Contains((x, z - 1)))
				AddUndirected(edges, new Vector2I(x, z), new Vector2I(x + 1, z));
			if (!occupied.Contains((x, z + 1)))
				AddUndirected(edges, new Vector2I(x, z + 1), new Vector2I(x + 1, z + 1));
			if (!occupied.Contains((x - 1, z)))
				AddUndirected(edges, new Vector2I(x, z), new Vector2I(x, z + 1));
			if (!occupied.Contains((x + 1, z)))
				AddUndirected(edges, new Vector2I(x + 1, z), new Vector2I(x + 1, z + 1));
		}

		return edges.ToList();
	}

	private static void AddUndirected(HashSet<(Vector2I A, Vector2I B)> edges, Vector2I a, Vector2I b)
	{
		if (a.X > b.X || (a.X == b.X && a.Y > b.Y))
			(a, b) = (b, a);
		edges.Add((a, b));
	}

	private static Vector3 CornerToWorld(Vector2I corner, int width, int height)
	{
		var origin = MapMapping.GridOrigin(width, height);
		return new Vector3(
			origin.X + corner.X * MapMapping.CellSize,
			0f,
			origin.Z + corner.Y * MapMapping.CellSize);
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

	private static Coord Centroid(IReadOnlySet<Coord> cells)
	{
		var x = 0;
		var z = 0;
		foreach (var cell in cells)
		{
			x += cell.X;
			z += cell.Z;
		}

		return new Coord(x / cells.Count, 0, z / cells.Count);
	}

	private static string TypeSlug(string id)
	{
		var dash = id.IndexOf('-');
		return dash <= 0 ? id : id[..dash];
	}
}
