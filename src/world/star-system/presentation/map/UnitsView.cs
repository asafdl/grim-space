using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Vision;

namespace GrimSpace.World.StarSystem.Presentation.Map;

public partial class UnitsView : Node3D
{
	// Ships are symbolic map icons; star scale comes from Star.DefaultRadius via MapView.
	private const float ShipLength = 0.05f;
	private const float ShipWidth = 0.028f;
	private const float RingRadius = 0.095f;
	private const float RingStroke = 0.012f;
	private const float RingAlpha = 0.42f;
	private const float PlayerRingScale = 1.45f;
	private static readonly Color PlayerColor = new(124f / 255f, 1f, 98f / 255f);
	private static readonly Color BeaconOutlineColor = new(0.08f, 0.10f, 0.12f, 0.95f);
	private const float BeaconMarkerWidth = 0.054f;
	private const float BeaconMarkerHeight = 0.062f;
	private const float BeaconOutlineScale = 1.18f;
	private const float BeaconStemWidth = 0.006f;
	private const float BeaconStemHeight = 0.50f;
	private const float BeaconHeight = 0.80f;
	private const int RingSegments = 24;
	private const float RingYOffset = 0.004f;
	private const float HullYOffset = 0.006f;
	private const float MarkerYOffset = 0.02f;
	private const float TrailYOffset = 0.012f;
	private const float TrailThickness = 0.0035f;
	private const float TrailMinStep = 0.006f;
	private const int TrailMaxPoints = 24;
	private const int UnitPickRadius = 14;

	private readonly Dictionary<string, UnitVisual> _units = new(StringComparer.Ordinal);
	private readonly Dictionary<string, List<Vector3>> _trailHistory = new(StringComparer.Ordinal);

	private int _width;
	private int _height;
	public sealed record UnitHoverInfo(
		string UnitId,
		EType Type,
		EFaction Faction,
		EDangerLevel? Danger);

	public void Build(StarMap world)
	{
		foreach (var child in GetChildren().ToArray())
		{
			RemoveChild(child);
			child.Free();
		}

		_units.Clear();
		_trailHistory.Clear();
		_width = world.Width;
		_height = world.Height;

		foreach (var unit in world.FleetRegistry.All.OrderBy(unit => unit.State.Id, StringComparer.Ordinal))
		{
			var unitVisual = BuildUnit(unit.State);
			_units[unit.State.Id] = unitVisual;
			AddChild(unitVisual.Root);
		}

	}

	public void Sync(
		StarSystemOrchestrator orchestrator,
		float tickFraction,
		Func<string, bool> isFleetVisible)
	{
		var world = orchestrator.Map;
		var registryIds = world.FleetRegistry.Ids.ToHashSet(StringComparer.Ordinal);

		foreach (var unitId in _units.Keys.Where(id => !registryIds.Contains(id)).ToList())
		{
			_units[unitId].Root.QueueFree();
			_units.Remove(unitId);
			_trailHistory.Remove(unitId);
		}

		foreach (var unit in world.FleetRegistry.All)
		{
			if (_units.ContainsKey(unit.State.Id))
				continue;

			var unitVisual = BuildUnit(unit.State);
			_units[unit.State.Id] = unitVisual;
			AddChild(unitVisual.Root);
		}

		foreach (var unit in world.FleetRegistry.All)
		{
			if (!_units.TryGetValue(unit.State.Id, out var unitVisual))
				continue;

			var visible = isFleetVisible(unit.State.Id);
			unitVisual.Root.Visible = visible;
			if (!visible)
			{
				_trailHistory.Remove(unit.State.Id);
				UpdateTrailSegments(unitVisual, null, Vector3.Zero, false);
				continue;
			}

			var sample = ResolveSample(world, unit, orchestrator.RuntimeFor(unit.State.Id), tickFraction);
			var mapPosition = MapMapping.ToWorld(sample.X, sample.Z, _width, _height);
			var worldPosition = mapPosition + Vector3.Up * MarkerYOffset;
			unitVisual.Marker.Position = worldPosition;
			unitVisual.Marker.Rotation = new Vector3(0f, sample.HeadingY, 0f);

			if (unitVisual.Beacon is { } beacon)
				beacon.Root.Position = worldPosition;

			var inTransit = unit.State.Phase == EPhase.InTransit;
			UpdateTrail(unit.State.Id, worldPosition, inTransit);
			UpdateTrailSegments(
				unitVisual,
				_trailHistory.GetValueOrDefault(unit.State.Id),
				worldPosition,
				inTransit);

		}
	}

	public UnitHoverInfo? UnitAt(
		StarSystemOrchestrator orchestrator,
		Coord point,
		float tickFraction,
		Func<string, bool> isFleetVisible)
	{
		var world = orchestrator.Map;
		UnitHoverInfo? best = null;
		var bestDistance = double.MaxValue;

		foreach (var unit in world.FleetRegistry.All)
		{
			if (!isFleetVisible(unit.State.Id))
				continue;

			var sample = ResolveSample(world, unit, orchestrator.RuntimeFor(unit.State.Id), tickFraction);
			var dx = point.X - sample.X;
			var dz = point.Z - sample.Z;
			var distance = dx * dx + dz * dz;
			if (distance > UnitPickRadius * UnitPickRadius || distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = new UnitHoverInfo(
				unit.State.Id,
				unit.State.Type,
				unit.State.Faction,
				unit.State.CombatProfile?.Danger);
		}

		return best;
	}

	private static UnitVisual BuildUnit(Units.State state)
	{
		var color = ColorForUnit(state);
		var isPlayer = state.Type == EType.PlayerFleet;
		var hullScale = isPlayer ? 1.35f : 1f;
		var ringScale = isPlayer ? PlayerRingScale : 1f;
		var ringRadius = RingRadius * ringScale;
		var ringStroke = RingStroke * ringScale;
		var shipLength = ShipLength * hullScale;
		var shipWidth = ShipWidth * hullScale;
		var root = new Node3D { Name = $"Unit_{state.Id}", Visible = false };
		var marker = new Node3D { Name = "Marker" };
		marker.AddChild(new MeshInstance3D
		{
			Name = "Ring",
			Position = new Vector3(0f, RingYOffset, 0f),
			Mesh = BuildRingMesh(ringRadius, ringStroke, RingSegments),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoColor = color with { A = RingAlpha },
				EmissionEnabled = true,
				Emission = color with { A = RingAlpha },
				EmissionEnergyMultiplier = 0.35f,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
		});
		marker.AddChild(new MeshInstance3D
		{
			Name = "Hull",
			Position = new Vector3(0f, HullYOffset, 0f),
			Mesh = BuildShipTriangleMesh(shipLength, shipWidth),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = color,
				EmissionEnabled = true,
				Emission = color,
				EmissionEnergyMultiplier = 0.45f,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
		});
		root.AddChild(marker);

		var beacon = isPlayer ? BuildPlayerBeacon(root, color) : null;

		var trailRoot = new Node3D { Name = "Trail" };
		var trailSegments = new MeshInstance3D[TrailMaxPoints];
		for (var i = 0; i < trailSegments.Length; i++)
		{
			var material = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoColor = color,
				EmissionEnabled = true,
				Emission = color,
				EmissionEnergyMultiplier = 0.40f,
			};

			var segment = new MeshInstance3D
			{
				Name = $"Segment_{i}",
				Visible = false,
				Mesh = new CylinderMesh
				{
					TopRadius = TrailThickness,
					BottomRadius = TrailThickness,
					Height = 1f,
					RadialSegments = 4,
					Rings = 1,
				},
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
				MaterialOverride = material,
			};
			trailRoot.AddChild(segment);
			trailSegments[i] = segment;
		}

		root.AddChild(trailRoot);

		return new UnitVisual(root, marker, trailSegments, beacon);
	}

	private static PlayerBeaconVisual BuildPlayerBeacon(Node3D unitRoot, Color color)
	{
		var beaconRoot = new Node3D { Name = "Beacon" };
		var stemCenterY = BeaconHeight - BeaconMarkerHeight * 0.5f - BeaconStemHeight * 0.5f;

		beaconRoot.AddChild(CreateBeaconMesh(
			"Stem",
			new QuadMesh { Size = new Vector2(BeaconStemWidth, BeaconStemHeight) },
			CreateBeaconMaterial(BeaconOutlineColor, renderPriority: 7),
			new Vector3(0f, stemCenterY, 0f)));

		var diamondAnchor = new Node3D
		{
			Name = "DiamondAnchor",
			Position = new Vector3(0f, BeaconHeight, 0f),
		};
		diamondAnchor.AddChild(CreateBeaconMesh(
			"Outline",
			BuildDownwardTriangleMesh(
				BeaconMarkerWidth * BeaconOutlineScale,
				BeaconMarkerHeight * BeaconOutlineScale),
			CreateBeaconMaterial(BeaconOutlineColor, renderPriority: 8)));

		diamondAnchor.AddChild(CreateBeaconMesh(
			"Marker",
			BuildDownwardTriangleMesh(BeaconMarkerWidth, BeaconMarkerHeight),
			CreateBeaconMaterial(color, renderPriority: 9)));
		beaconRoot.AddChild(diamondAnchor);

		unitRoot.AddChild(beaconRoot);
		return new PlayerBeaconVisual(beaconRoot);
	}

	private static MeshInstance3D CreateBeaconMesh(
		string name,
		Mesh mesh,
		StandardMaterial3D material,
		Vector3? position = null)
	{
		var instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = material,
		};
		if (position is { } localPosition)
			instance.Position = localPosition;
		return instance;
	}

	private static StandardMaterial3D CreateBeaconMaterial(Color color, int renderPriority) =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = 1.0f,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			BillboardKeepScale = true,
			DisableFog = true,
			NoDepthTest = true,
			RenderPriority = renderPriority,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};

	private void UpdateTrail(string unitId, Vector3 worldPosition, bool inTransit)
	{
		if (!inTransit)
		{
			_trailHistory.Remove(unitId);
			return;
		}

		if (!_trailHistory.TryGetValue(unitId, out var points))
		{
			points = [];
			_trailHistory[unitId] = points;
		}

		if (points.Count > 0 && points[^1].DistanceSquaredTo(worldPosition) < TrailMinStep * TrailMinStep)
			return;

		points.Add(worldPosition);
		if (points.Count > TrailMaxPoints)
			points.RemoveAt(0);
	}

	private static void UpdateTrailSegments(
		UnitVisual unitVisual,
		IReadOnlyList<Vector3>? points,
		Vector3 currentPosition,
		bool inTransit)
	{
		var segmentIndex = 0;
		if (inTransit && points is { Count: > 0 })
		{
			var totalSegments = points.Count;
			for (var i = 1; i < points.Count; i++, segmentIndex++)
			{
				var fade = i / (float)(totalSegments - 1);
				var alpha = Mathf.Lerp(0.10f, 0.55f, fade);
				PlaceTrailSegment(unitVisual.TrailSegments[segmentIndex], points[i - 1], points[i], alpha);
			}

			var tailFade = totalSegments == 1 ? 0.75f : 1f;
			PlaceTrailSegment(
				unitVisual.TrailSegments[segmentIndex],
				points[^1],
				currentPosition,
				Mathf.Lerp(0.55f, 0.75f, tailFade));
			segmentIndex++;
		}

		for (var i = segmentIndex; i < unitVisual.TrailSegments.Length; i++)
			unitVisual.TrailSegments[i].Visible = false;
	}

	private static void PlaceTrailSegment(
		MeshInstance3D segment,
		Vector3 from,
		Vector3 to,
		float alpha)
	{
		var start = from + Vector3.Up * TrailYOffset;
		var end = to + Vector3.Up * TrailYOffset;
		var delta = end - start;
		var length = delta.Length();
		if (length < 0.0005f)
		{
			segment.Visible = false;
			return;
		}

		var material = (StandardMaterial3D)segment.MaterialOverride!;
		var baseColor = material.AlbedoColor;
		material.AlbedoColor = baseColor with { A = alpha };
		material.Emission = baseColor with { A = alpha };

		segment.Visible = true;
		segment.Position = (start + end) * 0.5f;
		segment.Basis = BasisAlignedToDirection(delta);
		segment.Scale = new Vector3(1f, length, 1f);
	}

	private static Basis BasisAlignedToDirection(Vector3 direction)
	{
		var axis = direction.Normalized();
		if (axis.LengthSquared() < 0.0001f)
			return Basis.Identity;

		var reference = Mathf.Abs(axis.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;
		var tangent = reference.Cross(axis).Normalized();
		var bitangent = axis.Cross(tangent);
		return new Basis(tangent, axis, bitangent);
	}

	private static Color ColorForUnit(Units.State state)
	{
		if (state.Type == EType.PirateFleet && state.Faction == EFaction.Pirates)
			return new Color(0.72f, 0.22f, 0.58f);

		return ColorForType(state.Type);
	}

	private static Color ColorForType(EType type) =>
		type switch
		{
			EType.CargoShuttle => new Color(0.92f, 0.62f, 0.28f),
			EType.ComplianceVessel => new Color(0.58f, 0.62f, 0.92f),
			EType.ServiceVessel => new Color(0.42f, 0.72f, 0.88f),
			EType.PlayerFleet => PlayerColor,
			EType.Patrol => new Color(0.88f, 0.38f, 0.34f),
			EType.PirateFleet => new Color(0.72f, 0.22f, 0.58f),
			_ => new Color(0.75f, 0.75f, 0.75f),
		};

	private static ArrayMesh BuildDownwardTriangleMesh(float width, float height)
	{
		var halfWidth = width * 0.5f;
		var halfHeight = height * 0.5f;
		var vertices = new Vector3[]
		{
			new(-halfWidth, halfHeight, 0f),
			new(halfWidth, halfHeight, 0f),
			new(0f, -halfHeight, 0f),
		};
		var normals = new Vector3[] { Vector3.Back, Vector3.Back, Vector3.Back };
		var indices = new int[] { 0, 1, 2 };

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	private static ArrayMesh BuildRingMesh(float radius, float stroke, int segments)
	{
		var halfStroke = stroke * 0.5f;
		return BuildFlatRingMesh(radius - halfStroke, radius + halfStroke, segments);
	}

	private static ArrayMesh BuildFlatRingMesh(float innerRadius, float outerRadius, int segments)
	{
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

	private static ArrayMesh BuildShipTriangleMesh(float length, float width)
	{
		var halfLength = length * 0.5f;
		var halfWidth = width * 0.5f;
		var vertices = new Vector3[]
		{
			new(0f, 0f, halfLength),
			new(-halfWidth, 0f, -halfLength),
			new(halfWidth, 0f, -halfLength),
		};
		var normals = new Vector3[] { Vector3.Up, Vector3.Up, Vector3.Up };
		var indices = new int[] { 0, 1, 2 };

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	private static TrafficSample ResolveSample(
		StarMap world,
		Units.Fleet unit,
		Runtime.ActorRuntime runtime,
		float tickFraction)
	{
		var sample = FleetPositionSampler.Sample(world, unit.State, runtime, tickFraction);
		var heading = sample.Tangent is { } tangent
			? Mathf.Atan2(tangent.X * 0.001f, tangent.Z * 0.001f)
			: 0f;
		return new TrafficSample(sample.X, sample.Z, heading);
	}

	private sealed record UnitVisual(
		Node3D Root,
		Node3D Marker,
		MeshInstance3D[] TrailSegments,
		PlayerBeaconVisual? Beacon);

	private sealed record PlayerBeaconVisual(Node3D Root);

	private readonly record struct TrafficSample(double X, double Z, float HeadingY);
}
