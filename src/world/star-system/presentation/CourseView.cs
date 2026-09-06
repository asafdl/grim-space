using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class CourseView : Node3D
{
	private const float PathYOffset = 0.005f;
	private const float RingYOffset = 0.008f;
	private const float DestinationRingRadius = 0.11f;
	private const float DestinationRingStroke = 0.014f;
	private const int DestinationRingSegments = 28;

	private static readonly Color PathColor = new(0.35f, 0.85f, 0.95f, 0.55f);
	private static readonly Color DestinationRingColor = new(0.35f, 0.85f, 0.95f, 0.78f);
	private static readonly Color UnreachableRingColor = new(0.95f, 0.35f, 0.30f, 0.88f);

	private MeshInstance3D _pathMesh = null!;
	private MeshInstance3D _destinationRing = null!;
	private int _width;
	private int _height;
	private long _shownJourneyId;
	private TransitPath? _shownPath;

	public void Build(StarMap world)
	{
		_width = world.Width;
		_height = world.Height;
		_shownJourneyId = 0;
		_shownPath = null;

		foreach (var child in GetChildren().ToArray())
		{
			RemoveChild(child);
			child.Free();
		}

		_pathMesh = new MeshInstance3D
		{
			Name = "CoursePath",
			Visible = false,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				VertexColorUseAsAlbedo = true,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			},
		};
		AddChild(_pathMesh);

		_destinationRing = new MeshInstance3D
		{
			Name = "DestinationRing",
			Visible = false,
			Mesh = BuildRingMesh(DestinationRingRadius, DestinationRingStroke, DestinationRingSegments),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoColor = DestinationRingColor,
				EmissionEnabled = true,
				Emission = DestinationRingColor,
				EmissionEnergyMultiplier = 0.45f,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
		};
		AddChild(_destinationRing);
	}

	public void Sync(StarSystemOrchestrator orchestrator, bool unreachableFlash)
	{
		if (orchestrator.PlayerAgent?.PendingMove is { } pendingMove)
		{
			ShowCourse(pendingMove.Path, pendingMove.Destination, journeyId: 0, unreachableFlash);
			return;
		}

		var world = orchestrator.Map;
		var playerUnitId = orchestrator.PlayerId;
		if (playerUnitId is null
			|| !world.UnitRegistry.TryGet(playerUnitId, out var unit)
			|| unit.State.Phase != EPhase.InTransit
			|| orchestrator.RuntimeFor(playerUnitId).CachedPath is not { } path
			|| !unit.State.Journey.IsActive)
		{
			HideCourse();
			return;
		}

		ShowCourse(path, unit.State.Journey.Destination, unit.State.Journey.JourneyId, unreachableFlash);
	}

	private void ShowCourse(TransitPath path, Coord destination, long journeyId, bool unreachableFlash)
	{
		if (journeyId != 0 && journeyId != _shownJourneyId)
		{
			_shownJourneyId = journeyId;
			_shownPath = path;
			_pathMesh.Mesh = BuildPathMesh(path);
		}
		else if (journeyId == 0 && !ReferenceEquals(path, _shownPath))
		{
			_shownJourneyId = 0;
			_shownPath = path;
			_pathMesh.Mesh = BuildPathMesh(path);
		}

		_pathMesh.Visible = true;
		_destinationRing.Visible = true;
		_destinationRing.Position = ToWorld(destination) + Vector3.Up * RingYOffset;

		var ringColor = unreachableFlash ? UnreachableRingColor : DestinationRingColor;
		var material = (StandardMaterial3D)_destinationRing.MaterialOverride!;
		material.AlbedoColor = ringColor;
		material.Emission = ringColor;
	}

	private void HideCourse()
	{
		_shownJourneyId = 0;
		_shownPath = null;
		_pathMesh.Visible = false;
		_destinationRing.Visible = false;
	}

	private ImmediateMesh BuildPathMesh(TransitPath path)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(PathColor);

		foreach (var leg in path.Legs)
		{
			for (var i = 1; i < leg.Points.Length; i++)
			{
				mesh.SurfaceAddVertex(ToWorld(leg.Points[i - 1]));
				mesh.SurfaceAddVertex(ToWorld(leg.Points[i]));
			}
		}

		mesh.SurfaceEnd();
		return mesh;
	}

	private Vector3 ToWorld(Coord point) =>
		MapMapping.ToWorld(point, _width, _height) + Vector3.Up * PathYOffset;

	private static ArrayMesh BuildRingMesh(float radius, float stroke, int segments)
	{
		var halfStroke = stroke * 0.5f;
		var innerRadius = radius - halfStroke;
		var outerRadius = radius + halfStroke;
		var vertices = new Vector3[(segments + 1) * 2];
		var indices = new int[segments * 6];

		for (var i = 0; i <= segments; i++)
		{
			var angle = i * Mathf.Tau / segments;
			var cos = Mathf.Cos(angle);
			var sin = Mathf.Sin(angle);
			var inner = i * 2;
			vertices[inner] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
			vertices[inner + 1] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
		}

		var index = 0;
		for (var i = 0; i < segments; i++)
		{
			var baseVertex = i * 2;
			indices[index++] = baseVertex;
			indices[index++] = baseVertex + 1;
			indices[index++] = baseVertex + 2;
			indices[index++] = baseVertex + 1;
			indices[index++] = baseVertex + 3;
			indices[index++] = baseVertex + 2;
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}
}
