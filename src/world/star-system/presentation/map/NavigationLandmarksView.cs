using Godot;
using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Presentation.Picking;

namespace GrimSpace.World.StarSystem.Presentation.Map;

public partial class NavigationLandmarksView : Node3D
{
	private const int FootprintSegments = 48;
	private const float IndicatorClearancePadding = 0.08f;
	private const float HoverEmphasisScale = 1.04f;
	private static readonly Color FootprintColor = new(0.41f, 0.69f, 0.76f, 0.35f);
	private static readonly Color BoundsColor = new(0.82f, 0.62f, 0.38f, 0.42f);

	private readonly Dictionary<string, LandmarkVisual> _landmarks = new(StringComparer.Ordinal);
	private readonly List<RockAnimation> _rockAnimations = [];
	private IReadOnlyList<NavigationLandmark> _source = [];
	private string? _hoveredId;
	private int _width;
	private int _height;
	private bool _showNavigationFootprints;
	private bool _showLocalBounds;
	private bool _landmarksVisible = true;
	private float _animationTime;

	private readonly record struct RockAnimation(
		Node3D Target,
		Vector3 BaseRotation,
		Vector3 Axis,
		float RotationSpeed,
		float Phase);

	public bool ShowNavigationFootprints
	{
		get => _showNavigationFootprints;
		set
		{
			if (_showNavigationFootprints == value)
				return;

			_showNavigationFootprints = value;
			RefreshDebugOverlays();
		}
	}

	public bool ShowLocalBounds
	{
		get => _showLocalBounds;
		set
		{
			if (_showLocalBounds == value)
				return;

			_showLocalBounds = value;
			RefreshDebugOverlays();
		}
	}

	public bool LandmarksVisible
	{
		get => _landmarksVisible;
		set
		{
			_landmarksVisible = value;
			Visible = value;
		}
	}

	public override void _Process(double delta)
	{
		if (_rockAnimations.Count == 0)
			return;

		_animationTime += (float)delta;
		foreach (var animation in _rockAnimations)
		{
			var angle = animation.Phase + _animationTime * animation.RotationSpeed;
			animation.Target.Rotation =
				animation.BaseRotation + animation.Axis * angle;
		}
	}

	public void Build(StarMap world)
	{
		foreach (var child in GetChildren().ToArray())
		{
			RemoveChild(child);
			child.Free();
		}

		_landmarks.Clear();
		_rockAnimations.Clear();
		_hoveredId = null;
		_animationTime = 0f;
		_source = world.NavigationLandmarks;
		_width = world.Width;
		_height = world.Height;

		foreach (var landmark in _source.OrderBy(entry => entry.Id, StringComparer.Ordinal))
		{
			var root = NavigationLandmarkVisualCatalog.Build(landmark);
			root.Position = MapMapping.ToWorld(landmark.Position, _width, _height);
			RegisterRockAnimations(root, landmark.VisualSeed);
			_landmarks[landmark.Id] = new LandmarkVisual(root, landmark);
			AddChild(root);
		}

		RefreshDebugOverlays();
		Visible = _landmarksVisible;
	}

	private void RegisterRockAnimations(Node3D root, int visualSeed)
	{
		var random = new RandomNumberGenerator
		{
			Seed = StableSeedMixer.From(visualSeed).Add("landmark-motion").Value,
		};

		foreach (var rock in NavigationLandmarkVisualCatalog.FindAnimatedRockRoots(root))
		{
			var axis = new Vector3(
				random.Randf() - 0.5f,
				random.Randf() - 0.5f,
				random.Randf() - 0.5f).Normalized();
			_rockAnimations.Add(new RockAnimation(
				rock,
				rock.Rotation,
				axis,
				0.04f + random.Randf() * 0.08f,
				random.Randf() * Mathf.Tau));
		}
	}

	public string? PickAtScreen(MapInteractivePick.Context context)
	{
		string? bestId = null;
		var bestDirect = false;
		var bestDistance = float.MaxValue;

		foreach (var landmark in _source)
		{
			var direct = context.GridPoint is { } grid && ContainsPoint(landmark, grid);
			var worldPosition = MapMapping.ToWorld(landmark.Position, _width, _height);
			var screenDistance = MapScreenPick.DistancePixels(context.Camera, worldPosition, context.ScreenPos);
			if (!direct && screenDistance > MapScreenPick.SnapMarginPixels)
				continue;

			if (!MapScreenPick.IsBetterHit(direct, screenDistance, bestDirect, bestDistance))
				continue;

			bestDirect = direct;
			bestDistance = screenDistance;
			bestId = landmark.Id;
		}

		return bestId;
	}

	public string? LandmarkAt(Coord point)
	{
		string? bestId = null;
		var bestDistance = long.MaxValue;

		foreach (var landmark in _source)
		{
			if (!ContainsPoint(landmark, point))
				continue;

			var dx = point.X - landmark.Position.X;
			var dz = point.Z - landmark.Position.Z;
			var distance = (long)dx * dx + (long)dz * dz;
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestId = landmark.Id;
		}

		return bestId;
	}

	public void SetHovered(string? landmarkId)
	{
		if (string.Equals(_hoveredId, landmarkId, StringComparison.Ordinal))
			return;

		if (_hoveredId is { } previous && _landmarks.TryGetValue(previous, out var previousVisual))
			previousVisual.Root.Scale = Vector3.One;

		_hoveredId = landmarkId;
		if (landmarkId is not null && _landmarks.TryGetValue(landmarkId, out var visual))
			visual.Root.Scale = Vector3.One * HoverEmphasisScale;
	}

	public float GetIndicatorClearance(string landmarkId, StarMap world)
	{
		var landmark = world.NavigationLandmarks.FirstOrDefault(candidate => candidate.Id == landmarkId);
		return landmark is null
			? 0f
			: MapMapping.ToWorldRadius(landmark.Radius) + IndicatorClearancePadding;
	}

	public string? GetDisplayName(string landmarkId) =>
		_landmarks.TryGetValue(landmarkId, out var visual) ? visual.Landmark.DisplayName : null;

	private void RefreshDebugOverlays()
	{
		foreach (var visual in _landmarks.Values)
		{
			RemoveDebugChild(visual.Root, "DebugFootprint");
			RemoveDebugChild(visual.Root, "DebugBounds");

			if (_showNavigationFootprints)
				visual.Root.AddChild(BuildFootprintRing(visual.Landmark));

			if (_showLocalBounds)
				visual.Root.AddChild(BuildLocalBoundsOutline(visual.Root));
		}
	}

	private static void RemoveDebugChild(Node3D root, string name)
	{
		var existing = root.GetNodeOrNull(name);
		if (existing is null)
			return;

		root.RemoveChild(existing);
		existing.Free();
	}

	private MeshInstance3D BuildFootprintRing(NavigationLandmark landmark)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(FootprintColor);
		AddCircleOutline(mesh, Vector3.Zero, MapMapping.ToWorldRadius(landmark.Radius));
		mesh.SurfaceEnd();

		return new MeshInstance3D
		{
			Name = "DebugFootprint",
			Mesh = mesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateLineMaterial(),
		};
	}

	private static MeshInstance3D BuildLocalBoundsOutline(Node3D landmarkRoot)
	{
		var bounds = NavigationLandmarkVisualCatalog.MeasureLocalBounds(landmarkRoot);
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(BoundsColor);
		AddBoxOutline(mesh, bounds);
		mesh.SurfaceEnd();

		return new MeshInstance3D
		{
			Name = "DebugBounds",
			Mesh = mesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateLineMaterial(),
		};
	}

	private static StandardMaterial3D CreateLineMaterial() =>
		new()
		{
			VertexColorUseAsAlbedo = true,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
		};

	private static void AddCircleOutline(ImmediateMesh mesh, Vector3 center, float radius)
	{
		for (var i = 0; i < FootprintSegments; i++)
		{
			var a = i * Mathf.Tau / FootprintSegments;
			var b = (i + 1) * Mathf.Tau / FootprintSegments;
			mesh.SurfaceAddVertex(center + new Vector3(Mathf.Cos(a) * radius, 0.02f, Mathf.Sin(a) * radius));
			mesh.SurfaceAddVertex(center + new Vector3(Mathf.Cos(b) * radius, 0.02f, Mathf.Sin(b) * radius));
		}
	}

	private static void AddBoxOutline(ImmediateMesh mesh, Aabb bounds)
	{
		var min = bounds.Position;
		var max = bounds.Position + bounds.Size;
		var y = 0.03f;
		Vector3[] corners =
		[
			new(min.X, y, min.Z),
			new(max.X, y, min.Z),
			new(max.X, y, max.Z),
			new(min.X, y, max.Z),
		];

		for (var i = 0; i < corners.Length; i++)
		{
			var a = corners[i];
			var b = corners[(i + 1) % corners.Length];
			mesh.SurfaceAddVertex(a);
			mesh.SurfaceAddVertex(b);
		}
	}

	private static bool ContainsPoint(NavigationLandmark landmark, Coord point)
	{
		var dx = point.X - landmark.Position.X;
		var dz = point.Z - landmark.Position.Z;
		return (long)dx * dx + (long)dz * dz <= (long)landmark.Radius * landmark.Radius;
	}

	private sealed record LandmarkVisual(Node3D Root, NavigationLandmark Landmark);
}
