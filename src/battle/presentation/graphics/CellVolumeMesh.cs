using Godot;
using GrimSpace.Battle.Presentation;

namespace GrimSpace.Battle.Presentation.Graphics;

internal enum ECellVolumeMeshPrimitive
{
	Triangles,
	Wireframe,
}

internal static class CellVolumeMesh
{
	internal readonly record struct Prepared(
		ECellVolumeMeshPrimitive Primitive,
		Vector3[] Vertices,
		Vector3[] Normals,
		Color[] Colors);

	public static Prepared PrepareTriangles(CellVolumeGeometry.Surface surface) =>
		new(
			ECellVolumeMeshPrimitive.Triangles,
			surface.Vertices,
			surface.Normals,
			Enumerable.Repeat(Colors.White, surface.Vertices.Length).ToArray());

	public static Prepared PrepareWireframe(CellVolumeGeometry.Surface surface)
	{
		if (surface.Vertices.Length == 0)
			return new Prepared(ECellVolumeMeshPrimitive.Wireframe, [], [], []);

		var edges = new HashSet<MeshEdge>();
		for (var i = 0; i < surface.Vertices.Length; i += 3)
		{
			edges.Add(MeshEdge.Create(surface.Vertices[i], surface.Vertices[i + 1]));
			edges.Add(MeshEdge.Create(surface.Vertices[i + 1], surface.Vertices[i + 2]));
			edges.Add(MeshEdge.Create(surface.Vertices[i + 2], surface.Vertices[i]));
		}

		return new Prepared(
			ECellVolumeMeshPrimitive.Wireframe,
			edges.SelectMany(edge => new[] { edge.A, edge.B }).ToArray(),
			[],
			[]);
	}

	public static ArrayMesh Create(Prepared prepared)
	{
		var mesh = new ArrayMesh();
		if (prepared.Vertices.Length == 0)
			return mesh;

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = prepared.Vertices;
		if (prepared.Normals.Length > 0)
			arrays[(int)Mesh.ArrayType.Normal] = prepared.Normals;
		if (prepared.Colors.Length > 0)
			arrays[(int)Mesh.ArrayType.Color] = prepared.Colors;
		mesh.AddSurfaceFromArrays(
			prepared.Primitive == ECellVolumeMeshPrimitive.Triangles
				? Mesh.PrimitiveType.Triangles
				: Mesh.PrimitiveType.Lines,
			arrays);
		return mesh;
	}

	public static ArrayMesh CreateTriangles(CellVolumeGeometry.Surface surface) =>
		Create(PrepareTriangles(surface));

	public static ArrayMesh CreateWireframe(CellVolumeGeometry.Surface surface) =>
		Create(PrepareWireframe(surface));

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
	private readonly CellVolumeMeshStore _meshes;
	private string? _shapeKey;
	private bool _isExact;

	public CellVolumeWireframeSlot(
		string name,
		Material material,
		CellVolumeMeshStore meshes,
		CellVolumeGeometry.Settings? geometrySettings = null)
	{
		_geometrySettings = geometrySettings ?? GeometrySettings;
		_meshes = meshes;
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

	public void Apply(CellVolumePreview? volume, int tick)
	{
		if (volume is null)
		{
			Instance.Visible = false;
			return;
		}

		var shapeKey = CellVolumeGeometry.RelativeCellKey(
			volume.Origin,
			volume.Cells,
			_geometrySettings);
		if (_meshes.Request(
			volume,
			_geometrySettings,
			ECellVolumeMeshPrimitive.Wireframe,
			tick,
			out var exact))
		{
			Instance.Position = WorldMapping.ToWorld(volume.Origin);
			Instance.Basis = Basis.Identity;
			Instance.Visible = true;
			if (_isExact && shapeKey == _shapeKey)
				return;

			Instance.Mesh = exact;
			_shapeKey = shapeKey;
			_isExact = true;
			return;
		}

		Instance.Visible = false;
		_shapeKey = shapeKey;
		_isExact = false;
	}
}
