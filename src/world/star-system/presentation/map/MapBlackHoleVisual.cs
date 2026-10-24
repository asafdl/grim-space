using Godot;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;

namespace GrimSpace.World.StarSystem.Presentation.Map;

/// <summary>
/// The far-background black hole: an event-horizon silhouette wrapped in an
/// additive accretion disk, hung in real 3D space far beyond the playfield
/// (replaces the old flat <c>event-horizon.jpg</c> sky decal). Presentation
/// only — owned by <see cref="MapBackdrop"/> like the rest of the far stack.
/// </summary>
public static class MapBlackHoleVisual
{
	private const string HorizonShaderPath = "res://assets/shaders/black_hole_horizon.gdshader";
	private const string AccretionShaderPath = "res://assets/shaders/black_hole_accretion.gdshader";

	public static Node3D Create(MapAtmosphereSettings settings)
	{
		var direction = settings.BlackHoleDirection.Normalized();
		var root = new Node3D
		{
			Name = "BlackHole",
			Position = direction * settings.BlackHoleDistance,
		};

		root.AddChild(CreateHorizonSphere(settings, direction));
		root.AddChild(CreateAccretionDisk(settings, direction));
		return root;
	}

	private static MeshInstance3D CreateHorizonSphere(MapAtmosphereSettings settings, Vector3 direction)
	{
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(HorizonShaderPath),
		};
		material.SetShaderParameter("ring_feed", 0.8f);

		return new MeshInstance3D
		{
			Name = "EventHorizon",
			Mesh = new SphereMesh
			{
				Radius = settings.BlackHoleHorizonRadius,
				Height = settings.BlackHoleHorizonRadius * 2f,
				RadialSegments = 48,
				Rings = 24,
				Material = material,
			},
			// Never let the giant far object pop while orbiting.
			ExtraCullMargin = settings.BlackHoleDiskRadius,
		};
	}

	private static MeshInstance3D CreateAccretionDisk(MapAtmosphereSettings settings, Vector3 direction)
	{
		// Disk plane leans TOWARD the playfield: roughly perpendicular to the
		// map line, tilted off the camera axis so the ellipse opens instead of
		// collapsing to the knife-edge line the playfield stares straight down.
		var toMap = -direction;
		var edgeNormal = new Vector3(-toMap.Z, 0f, toMap.X).Normalized();
		var diskNormal = (edgeNormal + toMap * 0.72f).Normalized();
		// Slight roll for a calm diagonal rather than a flat ring.
		diskNormal = diskNormal.Rotated(toMap, Mathf.DegToRad(-18f)).Normalized();
		var yAxis = (toMap - diskNormal * toMap.Dot(diskNormal)).Normalized();
		var xAxis = yAxis.Cross(diskNormal).Normalized();

		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(AccretionShaderPath),
		};
		material.SetShaderParameter("inner_ratio", settings.BlackHoleHorizonRadius / settings.BlackHoleDiskRadius * 1.15f);
		material.SetShaderParameter("doppler_sign", 1.0f);

		return new MeshInstance3D
		{
			Name = "AccretionDisk",
			Mesh = new QuadMesh
			{
				Size = new Vector2(settings.BlackHoleDiskRadius * 2f, settings.BlackHoleDiskRadius * 2f),
				Material = material,
			},
			Basis = new Basis(xAxis, yAxis, diskNormal),
			ExtraCullMargin = settings.BlackHoleDiskRadius,
		};
	}
}
