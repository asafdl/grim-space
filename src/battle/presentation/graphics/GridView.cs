using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GridView : Node3D
{
private const float LocalGridViewDotThreshold = 0.9995f;
private const float VisibleFaceDotThreshold = 0.08f;

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
	internal enum LocalGridLineStyle { RearEdge, VisibleEdge, Hatch }
	internal readonly record struct StyledLine(LineSegment Segment, LocalGridLineStyle Style);

	private Camera3D _camera = null!;
	private MeshInstance3D _rangeShell = null!;
	private MeshInstance3D _localGrid = null!;
	private StandardMaterial3D _rangeShellMaterial = null!;

	private HashSet<Coord> _rangeEndpoints = [];
	private HashSet<Coord> _rangeCells = [];
	private readonly Dictionary<string, ArrayMesh> _rangeMeshes = [];
	private Coord? _rangeSource;
	private int? _rangeCacheTurn;
	private Vector3? _ghostWorld;
	private Vector3? _localGridViewDirection;

	public void Build(Camera3D camera)
	{
		_camera = camera;
		_rangeShellMaterial = CreateRangeShellMaterial();

		_rangeShell = CreateVisual(
			"MovementRangeShell",
			new ArrayMesh(),
			_rangeShellMaterial);
		AddChild(_rangeShell);

		_localGrid = CreateVisual(
			"MovementLocalGrid",
			CreateLocalGridMesh(CurrentViewDirection()),
			CreateLocalGridMaterial());
		_localGridViewDirection = CurrentViewDirection();
		AddChild(_localGrid);

		HideMoveVisuals();
	}

	public override void _Process(double delta)
	{
		if (_localGrid.Visible)
			RefreshLocalGridMesh();
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		if (_rangeCacheTurn != frame.TurnNumber)
		{
			_rangeMeshes.Clear();
			_rangeCacheTurn = frame.TurnNumber;
		}

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
		{
			_localGrid.GlobalPosition = ghostWorld;
			RefreshLocalGridMesh();
		}
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
		{
			var key = RangeMeshKey(source, cells);
			if (!_rangeMeshes.TryGetValue(key, out var mesh))
			{
				mesh = CreateRangeMesh(MovementRangeGeometry.Build(source, cells));
				_rangeMeshes[key] = mesh;
			}
			_rangeShell.Mesh = mesh;
		}

		PresentationDiagnostics.LogMoveRange(paths.Count, endpoints.Count);
	}

	internal static string RangeMeshKey(Coord source, IEnumerable<Coord> cells) =>
		string.Join(
			'|',
			cells
				.Select(cell => cell - source)
				.Distinct()
				.OrderBy(cell => cell.X)
				.ThenBy(cell => cell.Y)
				.ThenBy(cell => cell.Z)
				.Select(cell => $"{cell.X},{cell.Y},{cell.Z}"));

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

	private void HideMoveVisuals()
	{
		if (_rangeShell is not null)
			_rangeShell.Visible = false;
		if (_localGrid is not null)
			_localGrid.Visible = false;
		_ghostWorld = null;
	}

	internal static IReadOnlySet<LineSegment> CreateNeighborOutlineSegments()
		=> CreateNeighborOutlineEdges().Keys.ToHashSet();

	internal static IReadOnlyList<StyledLine> CreateCameraAwareLocalGridLines(
		Vector3 viewDirection)
	{
		viewDirection = viewDirection.Normalized();
		var lines = CreateNeighborOutlineEdges()
			.Select(edge => new StyledLine(
				edge.Key,
				edge.Value.Any(normal =>
					normal.Dot(viewDirection) > VisibleFaceDotThreshold)
					? LocalGridLineStyle.VisibleEdge
					: LocalGridLineStyle.RearEdge))
			.ToList();
		var half = WorldMapping.CellSize * 0.5f;

		foreach (var neighborOffset in NeighborOffsets)
		{
			var center = neighborOffset * WorldMapping.CellSize;
			foreach (var faceNormal in NeighborOffsets)
			{
				if (faceNormal.Dot(viewDirection) <= VisibleFaceDotThreshold)
					continue;

				var faceCenter = center + faceNormal * half;
				var (faceX, faceY) = FaceAxes(faceNormal);
				var diagonal = (faceX + faceY).Normalized();
				var offset = (faceX - faceY).Normalized()
					* WorldMapping.CellSize
					* 0.13f;
				var stroke = diagonal * WorldMapping.CellSize * 0.2f;
				lines.Add(new StyledLine(
					new LineSegment(faceCenter + offset - stroke, faceCenter + offset + stroke),
					LocalGridLineStyle.Hatch));
				lines.Add(new StyledLine(
					new LineSegment(faceCenter - offset - stroke, faceCenter - offset + stroke),
					LocalGridLineStyle.Hatch));
			}
		}

		return lines;
	}

	private static Dictionary<LineSegment, HashSet<Vector3>> CreateNeighborOutlineEdges()
	{
		var edges = new Dictionary<LineSegment, HashSet<Vector3>>();
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
							AddOutlineEdge(
								edges,
								new LineSegment(
									corner,
									corner + Vector3.Right * WorldMapping.CellSize),
								y * Vector3.Up,
								z * Vector3.Back);
						if (y < 0)
							AddOutlineEdge(
								edges,
								new LineSegment(
									corner,
									corner + Vector3.Up * WorldMapping.CellSize),
								x * Vector3.Right,
								z * Vector3.Back);
						if (z < 0)
							AddOutlineEdge(
								edges,
								new LineSegment(
									corner,
									corner + Vector3.Back * WorldMapping.CellSize),
								x * Vector3.Right,
								y * Vector3.Up);
					}
				}
			}
		}

		return edges;
	}

	private static void AddOutlineEdge(
		Dictionary<LineSegment, HashSet<Vector3>> edges,
		LineSegment segment,
		Vector3 faceNormalA,
		Vector3 faceNormalB)
	{
		if (!edges.TryGetValue(segment, out var faceNormals))
		{
			faceNormals = [];
			edges.Add(segment, faceNormals);
		}

		faceNormals.Add(faceNormalA);
		faceNormals.Add(faceNormalB);
	}

	private static (Vector3 X, Vector3 Y) FaceAxes(Vector3 normal)
	{
		if (Mathf.Abs(normal.X) > 0.5f)
			return (Vector3.Up, Vector3.Back);
		if (Mathf.Abs(normal.Y) > 0.5f)
			return (Vector3.Right, Vector3.Back);
		return (Vector3.Right, Vector3.Up);
	}

	private static ArrayMesh CreateLocalGridMesh(Vector3 viewDirection)
	{
		var lines = CreateCameraAwareLocalGridLines(viewDirection);
		var vertices = lines
			.SelectMany(line => new[] { line.Segment.From, line.Segment.To })
			.ToArray();
		var colors = lines
			.SelectMany(line =>
			{
				var color = LocalGridLineColor(line.Style);
				return new[] { color, color };
			})
			.ToArray();
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Color] = colors;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
		return mesh;
	}

	private void RefreshLocalGridMesh()
	{
		var viewDirection = CurrentViewDirection();
		if (_localGridViewDirection is Vector3 previous
			&& previous.Dot(viewDirection) >= LocalGridViewDotThreshold)
			return;

		_localGrid.Mesh = CreateLocalGridMesh(viewDirection);
		_localGridViewDirection = viewDirection;
	}

	private Vector3 CurrentViewDirection() =>
		_camera.GlobalTransform.Basis.Z.Normalized();

	private static Color LocalGridLineColor(LocalGridLineStyle style) =>
		style switch
		{
			LocalGridLineStyle.RearEdge => new Color(0.62f, 0.65f, 0.68f, 0.035f),
			LocalGridLineStyle.VisibleEdge => new Color(0.62f, 0.65f, 0.68f, 0.14f),
			LocalGridLineStyle.Hatch => new Color(0.62f, 0.65f, 0.68f, 0.12f),
			_ => throw new ArgumentOutOfRangeException(nameof(style), style, null),
		};

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

	private static StandardMaterial3D CreateLocalGridMaterial() =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = Colors.White,
			VertexColorUseAsAlbedo = true,
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
