using Godot;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class SpaceBackdrop : Node3D
{
	private const string StarsTexturePath = "res://assets/textures/8k_stars.jpg";
	private const string DustTexturePath = "res://assets/textures/particles/circle_05.png";
	private const string SmokeTexturePath = "res://assets/textures/particles/smoke_02.png";
	private const string PlanetTexturePath = "res://assets/textures/2k_venus_surface.jpg";
	private const ulong DustSeedSalt = 0xC6A4A7935BD1E995;
	private const float PlanetDistanceScale = 2.65f;
	private const float PlanetRadiusScale = 0.22f;

	public void Build(BoundedGrid grid, int seed)
	{
		var center = WorldMapping.GridCenter(grid);
		var halfExtent = new Vector3(
			grid.Width * WorldMapping.CellSize,
			grid.Height * WorldMapping.CellSize,
			grid.Depth * WorldMapping.CellSize) * 0.5f;
		var chamberRadius = halfExtent.Length();

		AddChild(CreateWorldEnvironment());
		AddChild(RedDwarfSun.CreateVisual(center, chamberRadius));
		AddChild(CreatePlanet(center, chamberRadius));
		AddChild(CreatePlanetLight(center));
		AddChild(CreateDustStream(center, halfExtent, seed));
	}

	private static WorldEnvironment CreateWorldEnvironment()
	{
		var stars = GD.Load<Texture2D>(StarsTexturePath);
		var sky = new Sky
		{
			SkyMaterial = new PanoramaSkyMaterial
			{
				Panorama = stars,
				EnergyMultiplier = 0.65f,
			},
			ProcessMode = Sky.ProcessModeEnum.Quality,
		};

		return new WorldEnvironment
		{
			Name = "SpaceEnvironment",
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Sky,
				Sky = sky,
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.12f, 0.14f, 0.18f),
				AmbientLightEnergy = 0.24f,
				ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				FogEnabled = false,
			},
		};
	}

	private static DirectionalLight3D CreatePlanetLight(Vector3 gridCenter) =>
		new()
		{
			Name = "PurplePlanetLight",
			Position = gridCenter,
			Basis = Basis.LookingAt(-RedDwarfSun.LightDirection, Vector3.Up),
			LightColor = new Color(0.58f, 0.38f, 0.86f),
			LightEnergy = 0.28f,
			LightCullMask = PresentationLayers.World,
			ShadowEnabled = false,
		};

	private static Node3D CreatePlanet(Vector3 gridCenter, float chamberRadius)
	{
		var radius = chamberRadius * PlanetRadiusScale;
		var root = new Node3D
		{
			Name = "PurplePlanet",
			Position = gridCenter + RedDwarfSun.LightDirection * chamberRadius * PlanetDistanceScale,
			RotationDegrees = new Vector3(-12f, 28f, 8f),
		};
		var texture = GD.Load<Texture2D>(PlanetTexturePath);
		root.AddChild(new MeshInstance3D
		{
			Name = "Surface",
			Mesh = CreateSphere(radius, radialSegments: 48, rings: 24),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreatePlanetSurfaceMaterial(texture),
		});
		root.AddChild(new MeshInstance3D
		{
			Name = "Atmosphere",
			Mesh = CreateSphere(radius * 1.06f, radialSegments: 48, rings: 24),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreatePlanetAtmosphereMaterial(),
		});
		return root;
	}

	private static SphereMesh CreateSphere(float radius, int radialSegments, int rings) =>
		new()
		{
			Radius = radius,
			Height = radius * 2f,
			RadialSegments = radialSegments,
			Rings = rings,
		};

	private static ShaderMaterial CreatePlanetSurfaceMaterial(Texture2D texture)
	{
		var material = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type spatial;

					uniform sampler2D surface_texture : source_color;

					void fragment()
					{
						vec3 source = texture(surface_texture, UV).rgb;
						float detail = dot(source, vec3(0.299, 0.587, 0.114));
						float tone = smoothstep(0.08, 0.95, detail);
						ALBEDO = mix(
							vec3(0.055, 0.018, 0.09),
							vec3(0.58, 0.23, 0.72),
							tone);
						ROUGHNESS = 0.94;
					}
					""",
			},
		};
		material.SetShaderParameter("surface_texture", texture);
		return material;
	}

	private static ShaderMaterial CreatePlanetAtmosphereMaterial() =>
		new()
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type spatial;

					render_mode
						unshaded,
						blend_add,
						depth_draw_never,
						cull_back,
						shadows_disabled,
						fog_disabled;

					void fragment()
					{
						float rim = pow(
							1.0 - max(dot(normalize(NORMAL), normalize(VIEW)), 0.0),
							2.2);
						vec3 haze = vec3(0.48, 0.16, 0.78);
						ALBEDO = haze;
						EMISSION = haze * rim * 0.45;
						ALPHA = rim * 0.34;
					}
					""",
			},
		};

	private static Node3D CreateDustStream(Vector3 gridCenter, Vector3 halfExtent, int seed)
	{
		var rng = new RandomNumberGenerator
		{
			Seed = (ulong)(uint)seed ^ DustSeedSalt,
		};
		var center = gridCenter + new Vector3(
			rng.RandfRange(-halfExtent.X, halfExtent.X),
			rng.RandfRange(-halfExtent.Y, halfExtent.Y),
			rng.RandfRange(-halfExtent.Z, halfExtent.Z)) * 0.18f;
		var direction = RandomUnit(rng);
		var up = Mathf.Abs(direction.Dot(Vector3.Up)) > 0.95f
			? Vector3.Right
			: Vector3.Up;
		var length = halfExtent.Length() * 2.5f;
		var width = Mathf.Min(halfExtent.X, Mathf.Min(halfExtent.Y, halfExtent.Z)) * 0.26f;
		var bend = width * rng.RandfRange(0.9f, 1.35f);
		if (rng.Randf() < 0.5f)
			bend = -bend;

		var root = new Node3D
		{
			Name = "DustStream",
			Position = center,
			Basis = Basis.LookingAt(direction, up),
		};
		root.AddChild(CreateDustLayer(
			"DiffuseDust",
			SmokeTexturePath,
			length,
			width,
			bend,
			thickness: width * 0.07f,
			amount: 1900,
			clusterCount: 22,
			particleSize: 2.2f,
			scale: new Vector2(0.65f, 1.6f),
			colors:
			[
				new Color(0.34f, 0.12f, 0.27f, 0.1f),
				new Color(0.2f, 0.13f, 0.34f, 0.09f),
				new Color(0.14f, 0.2f, 0.38f, 0.075f),
				new Color(0.42f, 0.18f, 0.16f, 0.085f),
			],
			rng: rng));
		root.AddChild(CreateDustLayer(
			"FineDust",
			DustTexturePath,
			length,
			width * 0.82f,
			bend,
			thickness: width * 0.045f,
			amount: 14000,
			clusterCount: 36,
			particleSize: 0.18f,
			scale: new Vector2(0.45f, 1.8f),
			colors:
			[
				new Color(0.95f, 0.42f, 0.68f, 0.48f),
				new Color(0.66f, 0.4f, 0.9f, 0.44f),
				new Color(0.42f, 0.58f, 0.95f, 0.38f),
				new Color(0.95f, 0.58f, 0.42f, 0.4f),
			],
			rng: rng));
		return root;
	}

	private static MultiMeshInstance3D CreateDustLayer(
		string name,
		string texturePath,
		float length,
		float width,
		float bend,
		float thickness,
		int amount,
		int clusterCount,
		float particleSize,
		Vector2 scale,
		IReadOnlyList<Color> colors,
		RandomNumberGenerator rng)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		var drawMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			AlbedoTexture = texture,
			VertexColorUseAsAlbedo = true,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			BillboardKeepScale = true,
		};
		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			InstanceCount = amount,
			Mesh = new QuadMesh
			{
				Size = Vector2.One * particleSize,
				Material = drawMaterial,
			},
		};
		var clusters = CreateDustClusters(length, width, bend, clusterCount, rng);
		var clusterColors = clusters
			.Select((_, index) => colors[index % colors.Count])
			.ToArray();
		for (var i = 0; i < amount; i++)
		{
			var clusterIndex = rng.RandiRange(0, clusters.Length - 1);
			var cluster = clusters[clusterIndex];
			var position = new Vector3(
				ClusterOffset(rng, cluster.X, width * 0.2f),
				ClusterOffset(rng, 0f, thickness),
				ClusterOffset(rng, cluster.Y, length / clusterCount * 1.2f));
			var instanceScale = rng.RandfRange(scale.X, scale.Y);
			var rotation = new Basis(Vector3.Forward, rng.RandfRange(0f, Mathf.Tau));
			multiMesh.SetInstanceTransform(
				i,
				new Transform3D(rotation.Scaled(Vector3.One * instanceScale), position));
			var color = clusterColors[clusterIndex];
			multiMesh.SetInstanceColor(
				i,
				color with { A = color.A * rng.RandfRange(0.45f, 1f) });
		}

		var layer = new MultiMeshInstance3D
		{
			Name = name,
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkWorld(layer);
		return layer;
	}

	private static Vector2[] CreateDustClusters(
		float length,
		float width,
		float bend,
		int count,
		RandomNumberGenerator rng)
	{
		var clusters = new Vector2[count];
		for (var i = 0; i < count; i++)
		{
			var t = (i + 0.5f) / count - 0.5f;
			clusters[i] = new Vector2(
				bend * Mathf.Sin(t * Mathf.Tau)
					+ rng.RandfRange(-width * 0.12f, width * 0.12f),
				Mathf.Lerp(-length * 0.5f, length * 0.5f, t + 0.5f)
					+ rng.RandfRange(-length / count, length / count));
		}
		return clusters;
	}

	private static float ClusterOffset(
		RandomNumberGenerator rng,
		float center,
		float spread) =>
		center + (rng.Randf() + rng.Randf() + rng.Randf() - 1.5f) * spread;

	private static Vector3 RandomUnit(RandomNumberGenerator rng)
	{
		var y = rng.RandfRange(-1f, 1f);
		var azimuth = rng.RandfRange(0f, Mathf.Tau);
		var radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
		return new Vector3(
			radius * Mathf.Cos(azimuth),
			y,
			radius * Mathf.Sin(azimuth));
	}
}
