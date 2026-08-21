using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Dark navigational void with a sparse distant starfield — no fog or nebulae.
/// </summary>
public sealed partial class MapBackdrop : Node3D
{
	private const int StarCount = 160;

	public void Build(float mapRadius = 16f)
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.008f, 0.02f, 0.05f), // ~#02050D
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.18f, 0.24f, 0.34f),
				AmbientLightEnergy = 0.12f,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				FogEnabled = false,
			},
		});

		AddChild(CreateStarfield(mapRadius));
	}

	/// <summary>
	/// Distant shell only. Skips the ecliptic slab over the map so stars don't sit on POIs.
	/// </summary>
	private static MultiMeshInstance3D CreateStarfield(float mapRadius)
	{
		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			InstanceCount = StarCount,
			Mesh = new SphereMesh { Radius = 0.05f, Height = 0.1f, RadialSegments = 4, Rings = 2 },
		};

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		var shellMin = mapRadius + 55f;
		var shellMax = mapRadius + 120f;
		var cylinderR = mapRadius + 22f;
		var eclipticClearance = 14f;
		var cylinderSq = cylinderR * cylinderR;

		for (var i = 0; i < StarCount; i++)
		{
			Vector3 position;
			var attempts = 0;
			do
			{
				var dir = RandomUnit(rng);
				var radius = rng.RandfRange(shellMin, shellMax);
				position = dir * radius;
				attempts++;
			} while (
				attempts < 32
				&& position.X * position.X + position.Z * position.Z < cylinderSq
				&& Mathf.Abs(position.Y) < eclipticClearance);

			var scale = rng.RandfRange(0.55f, 1.35f);
			multiMesh.SetInstanceTransform(
				i,
				new Transform3D(Basis.Identity.Scaled(Vector3.One * scale), position));

			// Mostly readable; a few brighter accents.
			var brightness = rng.Randf() < 0.06f
				? rng.RandfRange(0.45f, 0.70f)
				: rng.RandfRange(0.18f, 0.38f);

			var tint = rng.Randf();
			var color = tint switch
			{
				< 0.12f => new Color(0.70f, 0.80f, 1f, brightness),
				< 0.22f => new Color(1f, 0.90f, 0.78f, brightness),
				_ => new Color(0.92f, 0.95f, 1f, brightness),
			};
			multiMesh.SetInstanceColor(i, color);
		}

		return new MultiMeshInstance3D
		{
			Name = "Starfield",
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				VertexColorUseAsAlbedo = true,
				EmissionEnabled = true,
				Emission = Colors.White,
				EmissionEnergyMultiplier = 0.75f,
			},
		};
	}

	private static Vector3 RandomUnit(RandomNumberGenerator rng)
	{
		// Uniform direction on a sphere.
		var z = rng.RandfRange(-1f, 1f);
		var t = rng.RandfRange(0f, Mathf.Tau);
		var r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
		return new Vector3(r * Mathf.Cos(t), z, r * Mathf.Sin(t));
	}
}
