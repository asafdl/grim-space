using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class CellGridGeometry
{
	private const float VisibleFaceDotThreshold = 0.08f;

	internal readonly record struct LineSegment(Vector3 From, Vector3 To);
	internal enum LineStyle { RearEdge, VisibleEdge, Hatch }
	internal readonly record struct StyledLine(LineSegment Segment, LineStyle Style);

	public static IReadOnlyList<Vector3> NeighborCenters { get; } =
	[
		Vector3.Right * WorldMapping.CellSize,
		Vector3.Left * WorldMapping.CellSize,
		Vector3.Up * WorldMapping.CellSize,
		Vector3.Down * WorldMapping.CellSize,
		Vector3.Forward * WorldMapping.CellSize,
		Vector3.Back * WorldMapping.CellSize,
	];

	public static IReadOnlySet<LineSegment> CreateOutlineSegments(
		IEnumerable<Vector3> cellCenters) =>
		CreateOutlineEdges(cellCenters).Keys.ToHashSet();

	public static IReadOnlyList<StyledLine> CreateCameraAwareLines(
		Vector3 viewDirection,
		IEnumerable<Vector3> cellCenters,
		bool includeHatches)
	{
		var centers = cellCenters.ToArray();
		viewDirection = viewDirection.Normalized();
		var lines = CreateOutlineEdges(centers)
			.Select(edge => new StyledLine(
				edge.Key,
				edge.Value.Any(normal =>
					normal.Dot(viewDirection) > VisibleFaceDotThreshold)
					? LineStyle.VisibleEdge
					: LineStyle.RearEdge))
			.ToList();
		if (!includeHatches)
			return lines;

		var half = WorldMapping.CellSize * 0.5f;
		foreach (var center in centers)
		{
			foreach (var faceNormal in NeighborDirections)
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
					LineStyle.Hatch));
				lines.Add(new StyledLine(
					new LineSegment(faceCenter - offset - stroke, faceCenter - offset + stroke),
					LineStyle.Hatch));
			}
		}

		return lines;
	}

	public static ArrayMesh CreateMesh(
		Vector3 viewDirection,
		IEnumerable<Vector3> cellCenters,
		bool includeHatches)
	{
		var lines = CreateCameraAwareLines(viewDirection, cellCenters, includeHatches);
		var vertices = lines
			.SelectMany(line => new[] { line.Segment.From, line.Segment.To })
			.ToArray();
		var colors = lines
			.SelectMany(line =>
			{
				var color = LineColor(line.Style);
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

	public static StandardMaterial3D CreateMaterial(Color tint) =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = tint,
			VertexColorUseAsAlbedo = true,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
		};

	private static readonly Vector3[] NeighborDirections =
	[
		Vector3.Right,
		Vector3.Left,
		Vector3.Up,
		Vector3.Down,
		Vector3.Forward,
		Vector3.Back,
	];

	private static Dictionary<LineSegment, HashSet<Vector3>> CreateOutlineEdges(
		IEnumerable<Vector3> cellCenters)
	{
		var edges = new Dictionary<LineSegment, HashSet<Vector3>>();
		var half = WorldMapping.CellSize * 0.5f;
		foreach (var center in cellCenters.Distinct())
		{
			for (var x = -1; x <= 1; x += 2)
			{
				for (var y = -1; y <= 1; y += 2)
				{
					for (var z = -1; z <= 1; z += 2)
					{
						var corner = center + new Vector3(x, y, z) * half;
						if (x < 0)
						{
							AddOutlineEdge(
								edges,
								new LineSegment(
									corner,
									corner + Vector3.Right * WorldMapping.CellSize),
								y * Vector3.Up,
								z * Vector3.Back);
						}
						if (y < 0)
						{
							AddOutlineEdge(
								edges,
								new LineSegment(
									corner,
									corner + Vector3.Up * WorldMapping.CellSize),
								x * Vector3.Right,
								z * Vector3.Back);
						}
						if (z < 0)
						{
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

	private static Color LineColor(LineStyle style) =>
		style switch
		{
			LineStyle.RearEdge => new Color(0.62f, 0.65f, 0.68f, 0.035f),
			LineStyle.VisibleEdge => new Color(0.62f, 0.65f, 0.68f, 0.14f),
			LineStyle.Hatch => new Color(0.62f, 0.65f, 0.68f, 0.12f),
			_ => throw new ArgumentOutOfRangeException(nameof(style), style, null),
		};
}
