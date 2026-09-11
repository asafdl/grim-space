using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GridView : Node3D
{
private const float PathDotRadius = 0.126f;

	private static readonly Vector3[] NeighborOffsets =
	[
		Vector3.Right,
		Vector3.Left,
		Vector3.Up,
		Vector3.Down,
		Vector3.Forward,
		Vector3.Back,
	];

	internal readonly record struct LineSegment(Vector3 From, Vector3 To);

	private MeshInstance3D _rangeShell = null!;
	private MeshInstance3D _localGrid = null!;
	private StandardMaterial3D _rangeShellMaterial = null!;
	private StandardMaterial3D _pathMaterial = null!;
	private SphereMesh _pathMesh = null!;

	private readonly List<MeshInstance3D> _activePathMarkers = [];
	private readonly Queue<MeshInstance3D> _freePathMarkers = [];
	private HashSet<Coord> _rangeEndpoints = [];
	private HashSet<Coord> _rangeCells = [];
	private Coord? _rangeSource;
	private Vector3? _ghostWorld;

	public void Build()
	{
		_rangeShellMaterial = CreateRangeShellMaterial();
		_pathMaterial = CreatePathMaterial();
		_pathMesh = new SphereMesh
		{
			Radius = PathDotRadius,
			Height = PathDotRadius * 2f,
			RadialSegments = 12,
			Rings = 6,
		};

		_rangeShell = CreateVisual(
			"MovementRangeShell",
			new ArrayMesh(),
			_rangeShellMaterial);
		AddChild(_rangeShell);

		_localGrid = CreateVisual(
			"MovementLocalGrid",
			CreateLocalGridMesh(),
			CreateLocalGridMaterial());
		AddChild(_localGrid);

		HideMoveVisuals();
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		if (!frame.ShowMovePreview
			|| frame.ShowOutcomeOverlay
			|| frame.Mode != EPlayerMode.Move)
		{
			HideMoveVisuals();
			PresentationDiagnostics.LogMoveRange(0, 0);
			return;
		}

		ApplyMoveVisuals(frame);
	}

	private void ApplyMoveVisuals(PresentationFrame frame)
	{
		var source = frame.FocusState.Position;
		RefreshRange(source, frame.MovePaths);

		_rangeShell.GlobalPosition = WorldMapping.ToWorld(source);

		var showRange = _rangeEndpoints.Count > 0;
		_rangeShell.Visible = showRange;

		_ghostWorld = frame.MoveGhostState is { } ghost
			? WorldMapping.ToWorld(ghost.Position)
			: null;
		_localGrid.Visible = _ghostWorld is not null;
		if (_ghostWorld is Vector3 ghostWorld)
			_localGrid.GlobalPosition = ghostWorld;
		SetPathMarkers(frame.MovePath, frame.MoveGhostState?.Position);
	}

	private void RefreshRange(Coord source, IReadOnlyList<MovePathOption> paths)
	{
		var endpoints = paths.Select(option => option.EndPosition).ToHashSet();
		var cells = paths
			.SelectMany(option => option.Cells)
			.Append(source)
			.ToHashSet();
		if (_rangeSource == source
			&& _rangeEndpoints.SetEquals(endpoints)
			&& _rangeCells.SetEquals(cells))
			return;

		_rangeSource = source;
		_rangeEndpoints = endpoints;
		_rangeCells = cells;

		if (endpoints.Count > 0)
			_rangeShell.Mesh = CreateRangeMesh(
				MovementRangeGeometry.Build(source, cells));

		PresentationDiagnostics.LogMoveRange(paths.Count, endpoints.Count);
	}

	private static ArrayMesh CreateRangeMesh(MovementRangeGeometry.Surface surface)
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

	private void SetPathMarkers(IReadOnlyList<Coord> path, Coord? ghostPosition)
	{
		ReleasePathMarkers();

		foreach (var coord in path)
		{
			if (coord == ghostPosition)
				continue;

			var marker = AcquirePathMarker();
			marker.GlobalPosition = WorldMapping.ToWorld(coord);
			_activePathMarkers.Add(marker);
		}
	}

	private MeshInstance3D AcquirePathMarker()
	{
		if (_freePathMarkers.TryDequeue(out var marker))
		{
			marker.Visible = true;
			return marker;
		}

		marker = CreateVisual("MovementPathMarker", _pathMesh, _pathMaterial);
		AddChild(marker);
		return marker;
	}

	private void ReleasePathMarkers()
	{
		foreach (var marker in _activePathMarkers)
		{
			marker.Visible = false;
			_freePathMarkers.Enqueue(marker);
		}

		_activePathMarkers.Clear();
	}

	private void HideMoveVisuals()
	{
		if (_rangeShell is not null)
			_rangeShell.Visible = false;
		if (_localGrid is not null)
			_localGrid.Visible = false;
		_ghostWorld = null;
		ReleasePathMarkers();
	}

	internal static IReadOnlySet<LineSegment> CreateNeighborOutlineSegments()
	{
		var segments = new HashSet<LineSegment>();
		var half = WorldMapping.CellSize * 0.5f;

		foreach (var direction in NeighborOffsets)
		{
			var center = direction * WorldMapping.CellSize;
			for (var x = -1; x <= 1; x += 2)
			{
				for (var y = -1; y <= 1; y += 2)
				{
					for (var z = -1; z <= 1; z += 2)
					{
						var corner = center + new Vector3(x, y, z) * half;
						if (x < 0)
							segments.Add(new LineSegment(
								corner,
								corner + Vector3.Right * WorldMapping.CellSize));
						if (y < 0)
							segments.Add(new LineSegment(
								corner,
								corner + Vector3.Up * WorldMapping.CellSize));
						if (z < 0)
							segments.Add(new LineSegment(
								corner,
								corner + Vector3.Back * WorldMapping.CellSize));
					}
				}
			}
		}

		return segments;
	}

	internal static ArrayMesh CreateLocalGridMesh()
	{
		var vertices = CreateNeighborOutlineSegments()
			.SelectMany(segment => new[] { segment.From, segment.To })
			.ToArray();
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
		return mesh;
	}

	private static MeshInstance3D CreateVisual(
		string name,
		Mesh mesh,
		Material material)
	{
		var visual = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(visual);
		return visual;
	}

	private static StandardMaterial3D CreatePathMaterial() =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = new Color(0.45f, 0.5f, 0.6f, 0.45f),
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
		};

	private static StandardMaterial3D CreateLocalGridMaterial() =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = new Color(0.62f, 0.65f, 0.68f, 0.14f),
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
		};

	private static StandardMaterial3D CreateRangeShellMaterial() =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = new Color(0.32f, 0.78f, 0.56f, 0.03f),
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			NoDepthTest = false,
		};

}
