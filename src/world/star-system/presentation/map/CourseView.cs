using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Presentation.Map;

public partial class CourseView : Node3D
{
	private const string InterceptMarkerTexturePath = "res://assets/ui/map/intercept-marker.png";
	private const float PathYOffset = 0.005f;
	private const float RingYOffset = 0.008f;
	private const float MarkerYOffset = 0.009f;
	private const float DestinationRingRadius = 0.11f;
	private const float DestinationRingStroke = 0.014f;
	private const int DestinationRingSegments = 28;
	private const float InterceptMarkerSize = 0.15f;

	private static readonly Color PathColor = new(0.35f, 0.85f, 0.95f, 0.55f);
	private static readonly Color InterceptPathColor = new(0.95f, 0.35f, 0.38f, 0.48f);
	private static readonly Color DestinationRingColor = new(0.35f, 0.85f, 0.95f, 0.78f);
	private static readonly Color InterceptRingColor = new(1f, 0.15f, 0.22f, 0.95f);
	private static readonly Color UnreachableRingColor = new(0.95f, 0.35f, 0.30f, 0.88f);

	private MeshInstance3D _pathMesh = null!;
	private MeshInstance3D _destinationRing = null!;
	private MeshInstance3D _interceptMarker = null!;
	private int _width;
	private int _height;
	private long _shownJourneyId;
	private TransitPath? _shownPath;
	private TransitPath? _activeRemainingPath;
	private RouteSample _activeRemainingSample;

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

		var markerTexture = GD.Load<Texture2D>(InterceptMarkerTexturePath);
		_interceptMarker = new MeshInstance3D
		{
			Name = "InterceptMarker",
			Visible = false,
			Mesh = BuildFlatMarkerMesh(InterceptMarkerSize),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoTexture = markerTexture,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
		};
		AddChild(_interceptMarker);
	}

	public void Sync(StarSystemOrchestrator orchestrator, bool unreachableFlash, float tickFraction)
	{
		if (orchestrator.PlayerAgent?.PendingCourse is { } pendingCourse)
		{
			ShowCourse(
				pendingCourse.Path,
				pendingCourse.Destination,
				journeyId: 0,
				unreachableFlash,
				pendingCourse.DestinationKind);
			return;
		}

		var world = orchestrator.Map;
		var playerUnitId = orchestrator.PlayerId;
		if (playerUnitId is null
			|| !world.FleetRegistry.TryGet(playerUnitId, out var unit)
			|| unit.State.Travel is not FleetTravel.Journey journey
			|| orchestrator.RuntimeFor(playerUnitId).CachedPath is not { } path)
		{
			HideCourse();
			return;
		}

		var elapsed = world.Timeline.Clock.Current
			- journey.StartTick
			+ tickFraction;
		var sample = RoundedTransitRoute.For(path)
			.SampleAtElapsed(elapsed, unit.State.SpeedPerTick);

		var destinationKind = unit.State.TravelTarget.Kind == ETravelTargetKind.Fleet
			? ECourseDestinationKind.Intercept
			: ECourseDestinationKind.Move;

		ShowActiveCourse(path, sample, journey.Destination, unreachableFlash, destinationKind);
	}

	private void ShowCourse(
		TransitPath path,
		Coord destination,
		long journeyId,
		bool unreachableFlash,
		ECourseDestinationKind destinationKind)
	{
		var pathColor = PathColorFor(destinationKind);
		if (journeyId != 0 && journeyId != _shownJourneyId)
		{
			_shownJourneyId = journeyId;
			_shownPath = path;
			_pathMesh.Mesh = BuildPathMesh(path, pathColor);
		}
		else if (journeyId == 0 && !ReferenceEquals(path, _shownPath))
		{
			_shownJourneyId = 0;
			_shownPath = path;
			_activeRemainingPath = null;
			_pathMesh.Mesh = BuildPathMesh(path, pathColor);
		}

		_pathMesh.Visible = _pathMesh.Mesh is not null;
		ShowDestination(destination, unreachableFlash, destinationKind);
	}

	private void ShowActiveCourse(
		TransitPath path,
		RouteSample sample,
		Coord destination,
		bool unreachableFlash,
		ECourseDestinationKind destinationKind)
	{
		_shownJourneyId = 0;
		_shownPath = path;
		var pathColor = PathColorFor(destinationKind);
		if (!ReferenceEquals(path, _activeRemainingPath) || _activeRemainingSample != sample)
		{
			_activeRemainingPath = path;
			_activeRemainingSample = sample;
			_pathMesh.Mesh = BuildRemainingPathMesh(path, sample, pathColor);
		}

		_pathMesh.Visible = _pathMesh.Mesh is not null;
		ShowDestination(destination, unreachableFlash, destinationKind);
	}

	private void ShowDestination(
		Coord destination,
		bool unreachableFlash,
		ECourseDestinationKind destinationKind)
	{
		var worldPosition = ToWorld(destination) + Vector3.Up * RingYOffset;
		_destinationRing.Position = worldPosition;
		_destinationRing.Visible = true;

		var isIntercept = destinationKind == ECourseDestinationKind.Intercept;
		var ringColor = unreachableFlash && !isIntercept
			? UnreachableRingColor
			: isIntercept
				? InterceptRingColor
				: DestinationRingColor;

		var material = (StandardMaterial3D)_destinationRing.MaterialOverride!;
		material.AlbedoColor = ringColor;
		material.Emission = ringColor;
		material.EmissionEnergyMultiplier = isIntercept ? 0.65f : 0.45f;

		_interceptMarker.Visible = isIntercept;
		if (isIntercept)
			_interceptMarker.Position = ToWorld(destination) + Vector3.Up * MarkerYOffset;
	}

	private void HideCourse()
	{
		_shownJourneyId = 0;
		_shownPath = null;
		_activeRemainingPath = null;
		_pathMesh.Visible = false;
		_destinationRing.Visible = false;
		_interceptMarker.Visible = false;
	}

	private static Color PathColorFor(ECourseDestinationKind destinationKind) =>
		destinationKind == ECourseDestinationKind.Intercept ? InterceptPathColor : PathColor;

	private ImmediateMesh? BuildPathMesh(TransitPath path, Color pathColor)
	{
		var route = RoundedTransitRoute.For(path);
		var points = route.Points;
		if (points.Count < 2)
			return null;

		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(pathColor);
		for (var i = 1; i < points.Count; i++)
		{
			mesh.SurfaceAddVertex(ToWorld(points[i - 1].X, points[i - 1].Z));
			mesh.SurfaceAddVertex(ToWorld(points[i].X, points[i].Z));
		}

		mesh.SurfaceEnd();
		return mesh;
	}

	private ImmediateMesh? BuildRemainingPathMesh(TransitPath path, RouteSample sample, Color pathColor)
	{
		var points = RoundedTransitRoute.For(path).RemainingPoints(sample);
		if (points.Count < 2)
			return null;

		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(pathColor);

		for (var i = 1; i < points.Count; i++)
		{
			mesh.SurfaceAddVertex(ToWorld(points[i - 1].X, points[i - 1].Z));
			mesh.SurfaceAddVertex(ToWorld(points[i].X, points[i].Z));
		}

		mesh.SurfaceEnd();
		return mesh;
	}

	private Vector3 ToWorld(Coord point) =>
		MapMapping.ToWorld(point, _width, _height) + Vector3.Up * PathYOffset;

	private Vector3 ToWorld(double x, double z) =>
		MapMapping.ToWorld(x, z, _width, _height) + Vector3.Up * PathYOffset;

	private static PlaneMesh BuildFlatMarkerMesh(float size) =>
		new() { Size = new Vector2(size, size) };

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
