using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapView : Node3D
{
	private readonly Dictionary<Coord, string> _cellOwners = new();
	private int _width;
	private int _height;
	private Label3D? _hoverLabel;

	public void Build(StarMap world)
	{
		foreach (var child in GetChildren().ToArray())
		{
			RemoveChild(child);
			child.Free();
		}
		_cellOwners.Clear();
		_hoverLabel = null;
		_width = world.Width;
		_height = world.Height;

		AddChild(BuildGridLines(world.Width, world.Height));

		foreach (var poi in world.PointsOfInterest)
		{
			foreach (var cell in poi.Cells)
				_cellOwners[cell] = poi.Id;

			AddChild(BuildPoiFootprint(poi, world.Width, world.Height));
			AddChild(BuildPoiMarker(poi, world.Width, world.Height));
		}

		_hoverLabel = new Label3D
		{
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = 48,
			OutlineSize = 8,
			Modulate = Colors.White,
			Visible = false,
			NoDepthTest = true,
		};
		AddChild(_hoverLabel);
	}

	public void SetHovered(string? poiId, Coord? cell)
	{
		if (_hoverLabel is null)
			return;

		if (poiId is null || cell is null)
		{
			_hoverLabel.Visible = false;
			return;
		}

		_hoverLabel.Text = poiId;
		_hoverLabel.Position = MapMapping.ToWorld(cell.Value, _width, _height) + Vector3.Up * 1.2f;
		_hoverLabel.Visible = true;
	}

	public string? OwnerOf(Coord cell) =>
		_cellOwners.GetValueOrDefault(cell);

	private static MeshInstance3D BuildGridLines(int width, int height)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(new Color(0.35f, 0.4f, 0.5f, 0.35f));

		var origin = MapMapping.GridOrigin(width, height);
		var size = MapMapping.CellSize;

		for (var x = 0; x <= width; x++)
		{
			var wx = origin.X + x * size;
			mesh.SurfaceAddVertex(new Vector3(wx, 0.01f, origin.Z));
			mesh.SurfaceAddVertex(new Vector3(wx, 0.01f, origin.Z + height * size));
		}

		for (var z = 0; z <= height; z++)
		{
			var wz = origin.Z + z * size;
			mesh.SurfaceAddVertex(new Vector3(origin.X, 0.01f, wz));
			mesh.SurfaceAddVertex(new Vector3(origin.X + width * size, 0.01f, wz));
		}

		mesh.SurfaceEnd();

		return new MeshInstance3D
		{
			Mesh = mesh,
			MaterialOverride = new StandardMaterial3D
			{
				VertexColorUseAsAlbedo = true,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			},
		};
	}

	private static MeshInstance3D BuildPoiFootprint(PointOfInterest poi, int width, int height)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(ColorFor(poi.Id));

		foreach (var cell in poi.Cells)
		{
			var min = MapMapping.CellOrigin(cell, width, height) + new Vector3(0f, 0.05f, 0f);
			var max = min + new Vector3(MapMapping.CellSize, 0f, MapMapping.CellSize);
			AddQuadOutline(mesh, min, max);
		}

		mesh.SurfaceEnd();

		return new MeshInstance3D
		{
			Name = $"Footprint_{poi.Id}",
			Mesh = mesh,
			MaterialOverride = new StandardMaterial3D
			{
				VertexColorUseAsAlbedo = true,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			},
		};
	}

	private static MeshInstance3D BuildPoiMarker(PointOfInterest poi, int width, int height)
	{
		var type = TypeSlug(poi.Id);
		var (mesh, y) = type switch
		{
			"star" => ((Mesh)new SphereMesh { Radius = 1.4f, Height = 2.8f }, 1.4f),
			"planet" => (new SphereMesh { Radius = 0.7f, Height = 1.4f }, 0.7f),
			_ => (new BoxMesh { Size = new Vector3(0.55f, 0.55f, 0.55f) }, 0.35f),
		};

		var centroid = Centroid(poi.Cells);
		return new MeshInstance3D
		{
			Name = $"Marker_{poi.Id}",
			Mesh = mesh,
			Position = MapMapping.ToWorld(centroid, width, height) + Vector3.Up * y,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = ColorFor(poi.Id),
				EmissionEnabled = type == "star",
				Emission = new Color(1f, 0.85f, 0.35f),
				EmissionEnergyMultiplier = type == "star" ? 2.5f : 0f,
			},
		};
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

	private static Color ColorFor(string id) =>
		TypeSlug(id) switch
		{
			"star" => new Color(1f, 0.85f, 0.35f),
			"planet" => new Color(0.35f, 0.65f, 1f),
			"station" => new Color(0.75f, 0.75f, 0.8f),
			_ => new Color(0.8f, 0.5f, 0.9f),
		};
}
