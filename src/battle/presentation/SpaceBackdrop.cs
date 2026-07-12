using Godot;

namespace GrimSpace.Battle.Presentation;

public sealed partial class SpaceBackdrop : Node3D
{
	private const int StarCount = 1400;
	private const float StarRadius = 380f;

	public override void _Ready()
	{
		AddChild(CreateWorldEnvironment());
		AddChild(CreateStarfield());
	}

	private static WorldEnvironment CreateWorldEnvironment()
	{
		var environment = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.008f, 0.01f, 0.03f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.12f, 0.14f, 0.22f),
			AmbientLightEnergy = 0.55f,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
		};

		return new WorldEnvironment { Environment = environment };
	}

	private Node3D CreateStarfield()
	{
		var root = new Node3D();

		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			InstanceCount = StarCount,
			Mesh = new SphereMesh { Radius = 0.08f, Height = 0.16f, RadialSegments = 4, Rings = 2 },
		};

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		for (var i = 0; i < StarCount; i++)
		{
			var direction = RandomUnitVector(rng);
			var distance = StarRadius * rng.RandfRange(0.85f, 1f);
			var scale = rng.RandfRange(0.6f, 2.4f);
			var brightness = rng.RandfRange(0.35f, 1f);
			var tint = rng.Randf();

			multiMesh.SetInstanceTransform(
				i,
				new Transform3D(Basis.Identity.Scaled(Vector3.One * scale), direction * distance));

			var color = tint switch
			{
				< 0.15f => new Color(0.75f, 0.85f, 1f, brightness),
				< 0.3f => new Color(1f, 0.92f, 0.8f, brightness),
				_ => new Color(1f, 1f, 1f, brightness),
			};
			multiMesh.SetInstanceColor(i, color);
		}

		var stars = new MultiMeshInstance3D
		{
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				VertexColorUseAsAlbedo = true,
				EmissionEnabled = true,
				Emission = Colors.White,
				EmissionEnergyMultiplier = 1.6f,
			},
		};
		root.AddChild(stars);

		return root;
	}

	private static Vector3 RandomUnitVector(RandomNumberGenerator rng)
	{
		var z = rng.RandfRange(-1f, 1f);
		var theta = rng.RandfRange(0f, Mathf.Tau);
		var ring = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
		return new Vector3(ring * Mathf.Cos(theta), z, ring * Mathf.Sin(theta));
	}
}
