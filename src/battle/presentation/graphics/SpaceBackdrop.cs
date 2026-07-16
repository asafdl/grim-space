using Godot;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class SpaceBackdrop : Node3D
{
	private const int StarCount = 700;
	private const int InteriorWispCount = 10;
	private const float BoundaryPadding = 4f;

	public void Build(BoundedGrid grid)
	{
		var center = WorldMapping.GridCenter(grid);
		var extent = GridExtent(grid);
		var half = extent * 0.5f;

		AddChild(CreateWorldEnvironment(half.Length()));
		AddChild(CreateNebulaShell(center, half));
		AddChild(CreateStarfield(center, half));
		AddChild(RedDwarfSun.CreateVisual(center, half.Length()));
	}

	private static Vector3 GridExtent(BoundedGrid grid) =>
		new(
			grid.Width * WorldMapping.CellSize,
			grid.Height * WorldMapping.CellSize,
			grid.Depth * WorldMapping.CellSize);

	private static WorldEnvironment CreateWorldEnvironment(float chamberRadius)
	{
		var environment = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.02f, 0.015f, 0.04f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.14f, 0.1f, 0.12f),
			AmbientLightEnergy = 0.28f,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
			FogEnabled = true,
			FogMode = Godot.Environment.FogModeEnum.Depth,
			FogLightColor = new Color(0.35f, 0.22f, 0.55f),
			FogLightEnergy = 0.35f,
			FogDensity = 0.018f,
			FogDepthBegin = chamberRadius * 0.25f,
			FogDepthEnd = chamberRadius * 1.15f,
			FogAerialPerspective = 0.35f,
		};

		return new WorldEnvironment { Environment = environment };
	}

	private static Node3D CreateNebulaShell(Vector3 center, Vector3 half)
	{
		var root = new Node3D { Position = center };
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		AddFaceWisps(root, half, rng);
		AddCornerWisps(root, half, rng);
		AddInteriorWisps(root, half, rng);
		AddVoidMembrane(root, half);

		return root;
	}

	private static void AddFaceWisps(Node3D root, Vector3 half, RandomNumberGenerator rng)
	{
		var faces = new[]
		{
			(new Vector3(half.X, 0f, 0f), new Vector3(0.55f, 1f, 1f)),
			(new Vector3(-half.X, 0f, 0f), new Vector3(0.55f, 1f, 1f)),
			(new Vector3(0f, half.Y, 0f), new Vector3(1f, 0.45f, 1f)),
			(new Vector3(0f, -half.Y, 0f), new Vector3(1f, 0.45f, 1f)),
			(new Vector3(0f, 0f, half.Z), new Vector3(1f, 1f, 0.5f)),
			(new Vector3(0f, 0f, -half.Z), new Vector3(1f, 1f, 0.5f)),
		};

		foreach (var (offset, stretch) in faces)
		{
			for (var i = 0; i < 2; i++)
			{
				var jitter = new Vector3(
					rng.RandfRange(-half.X * 0.35f, half.X * 0.35f),
					rng.RandfRange(-half.Y * 0.35f, half.Y * 0.35f),
					rng.RandfRange(-half.Z * 0.35f, half.Z * 0.35f));
				var color = NebulaColor(rng, 0.18f, 0.32f);
				root.AddChild(CreateWisp(
					offset + jitter,
					new Vector3(18f, 14f, 16f) * stretch * rng.RandfRange(0.85f, 1.15f),
					color,
					rng.RandfRange(0.16f, 0.28f)));
			}
		}
	}

	private static void AddCornerWisps(Node3D root, Vector3 half, RandomNumberGenerator rng)
	{
		foreach (var sx in new[] { -1f, 1f })
		{
			foreach (var sy in new[] { -1f, 1f })
			{
				foreach (var sz in new[] { -1f, 1f })
				{
					var offset = new Vector3(sx * half.X * 0.82f, sy * half.Y * 0.82f, sz * half.Z * 0.82f);
					var color = NebulaColor(rng, 0.22f, 0.38f);
					root.AddChild(CreateWisp(
						offset,
						Vector3.One * rng.RandfRange(20f, 30f),
						color,
						rng.RandfRange(0.2f, 0.34f)));
				}
			}
		}
	}

	private static void AddInteriorWisps(Node3D root, Vector3 half, RandomNumberGenerator rng)
	{
		for (var i = 0; i < InteriorWispCount; i++)
		{
			var offset = new Vector3(
				rng.RandfRange(-half.X * 0.55f, half.X * 0.55f),
				rng.RandfRange(-half.Y * 0.55f, half.Y * 0.55f),
				rng.RandfRange(-half.Z * 0.55f, half.Z * 0.55f));
			var color = NebulaColor(rng, 0.08f, 0.16f);
			root.AddChild(CreateWisp(
				offset,
				Vector3.One * rng.RandfRange(8f, 16f),
				color,
				rng.RandfRange(0.06f, 0.12f)));
		}
	}

	private static void AddVoidMembrane(Node3D root, Vector3 half)
	{
		var membrane = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = half * 2f + Vector3.One * BoundaryPadding },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				CullMode = BaseMaterial3D.CullModeEnum.Front,
				AlbedoColor = new Color(0.04f, 0.03f, 0.08f, 0.55f),
				EmissionEnabled = true,
				Emission = new Color(0.18f, 0.08f, 0.28f),
				EmissionEnergyMultiplier = 0.25f,
			},
		};
		root.AddChild(membrane);
	}

	private static MeshInstance3D CreateWisp(Vector3 offset, Vector3 scale, Color color, float alpha)
	{
		return new MeshInstance3D
		{
			Position = offset,
			Scale = scale,
			Mesh = new SphereMesh { Radius = 1f, RadialSegments = 12, Rings = 8 },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Add,
				AlbedoColor = color with { A = alpha },
				EmissionEnabled = true,
				Emission = color,
				EmissionEnergyMultiplier = 0.9f,
			},
		};
	}

	private static Color NebulaColor(RandomNumberGenerator rng, float minAlpha, float maxAlpha)
	{
		var palette = rng.Randf() switch
		{
			< 0.34f => new Color(0.45f, 0.18f, 0.72f),
			< 0.67f => new Color(0.15f, 0.42f, 0.78f),
			_ => new Color(0.72f, 0.22f, 0.48f),
		};
		return palette with { A = rng.RandfRange(minAlpha, maxAlpha) };
	}

	private static Node3D CreateStarfield(Vector3 center, Vector3 half)
	{
		var root = new Node3D { Position = center };

		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			InstanceCount = StarCount,
			Mesh = new SphereMesh { Radius = 0.06f, Height = 0.12f, RadialSegments = 4, Rings = 2 },
		};

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		var inner = half * 0.72f;

		for (var i = 0; i < StarCount; i++)
		{
			var position = new Vector3(
				rng.RandfRange(-inner.X, inner.X),
				rng.RandfRange(-inner.Y, inner.Y),
				rng.RandfRange(-inner.Z, inner.Z));
			var scale = rng.RandfRange(0.5f, 1.8f);
			var brightness = rng.RandfRange(0.25f, 0.85f);
			var tint = rng.Randf();

			multiMesh.SetInstanceTransform(
				i,
				new Transform3D(Basis.Identity.Scaled(Vector3.One * scale), position));

			var color = tint switch
			{
				< 0.15f => new Color(0.75f, 0.85f, 1f, brightness),
				< 0.3f => new Color(1f, 0.92f, 0.8f, brightness),
				_ => new Color(1f, 1f, 1f, brightness * 0.8f),
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
				EmissionEnergyMultiplier = 1.2f,
			},
		};
		root.AddChild(stars);

		return root;
	}
}
