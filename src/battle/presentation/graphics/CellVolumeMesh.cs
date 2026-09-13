using Godot;
using GrimSpace.Battle.Presentation;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class CellVolumeMesh
{
	public static ArrayMesh CreateTriangles(CellVolumeGeometry.Surface surface)
	{
		var mesh = new ArrayMesh();
		if (surface.Vertices.Length == 0)
			return mesh;

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = surface.Vertices;
		arrays[(int)Mesh.ArrayType.Normal] = surface.Normals;
		arrays[(int)Mesh.ArrayType.Color] = Enumerable
			.Repeat(Colors.White, surface.Vertices.Length)
			.ToArray();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	public static ArrayMesh CreateWireframe(CellVolumeGeometry.Surface surface)
	{
		var mesh = new ArrayMesh();
		if (surface.Vertices.Length == 0)
			return mesh;

		var edges = new HashSet<MeshEdge>();
		for (var i = 0; i < surface.Vertices.Length; i += 3)
		{
			edges.Add(MeshEdge.Create(surface.Vertices[i], surface.Vertices[i + 1]));
			edges.Add(MeshEdge.Create(surface.Vertices[i + 1], surface.Vertices[i + 2]));
			edges.Add(MeshEdge.Create(surface.Vertices[i + 2], surface.Vertices[i]));
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = edges
			.SelectMany(edge => new[] { edge.A, edge.B })
			.ToArray();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
		return mesh;
	}

	private readonly record struct MeshEdge(Vector3 A, Vector3 B)
	{
		public static MeshEdge Create(Vector3 a, Vector3 b) =>
			Compare(a, b) <= 0 ? new MeshEdge(a, b) : new MeshEdge(b, a);

		private static int Compare(Vector3 a, Vector3 b)
		{
			var x = a.X.CompareTo(b.X);
			if (x != 0)
				return x;
			var y = a.Y.CompareTo(b.Y);
			return y != 0 ? y : a.Z.CompareTo(b.Z);
		}
	}
}

internal sealed class CellVolumeWireframeSlot
{
	internal static readonly CellVolumeGeometry.Settings GeometrySettings =
		new(5, 0.42, 0.85, 24, 0.4);

	private readonly CellVolumeGeometry.Settings _geometrySettings;
	private string? _shapeKey;

	public CellVolumeWireframeSlot(
		string name,
		Material material,
		CellVolumeGeometry.Settings? geometrySettings = null)
	{
		_geometrySettings = geometrySettings ?? GeometrySettings;
		Instance = new MeshInstance3D
		{
			Name = name,
			Mesh = new ArrayMesh(),
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Visible = false,
		};
		PresentationLayers.MarkUx(Instance);
	}

	public MeshInstance3D Instance { get; }

	public void Apply(CellVolumePreview? volume)
	{
		Instance.Visible = volume is not null;
		if (volume is null)
			return;

		Instance.Position = WorldMapping.ToWorld(volume.Origin);
		Instance.Basis = Basis.Identity;
		var shapeKey = CellVolumeGeometry.RelativeCellKey(
			volume.Origin,
			volume.Cells,
			_geometrySettings);
		if (shapeKey == _shapeKey)
			return;

		Instance.Mesh = CellVolumeMesh.CreateWireframe(
			CellVolumeGeometry.Build(volume.Origin, volume.Cells, _geometrySettings));
		_shapeKey = shapeKey;
	}
}
