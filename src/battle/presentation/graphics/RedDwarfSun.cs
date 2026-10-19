using Godot;
using GrimSpace.Presentation.Graphics;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class RedDwarfSun
{
	private const string SurfaceTexturePath = "res://assets/textures/2k_sun.jpg";
	private const string SurfaceShaderPath =
		"res://assets/shaders/battle_red_dwarf.gdshader";
	private const float DistanceScale = 2.4f;
	private const float RadiusScale = 0.72f;

	public static readonly Vector3 LightDirection = new Vector3(-0.58f, -0.22f, -0.78f).Normalized();
	public static readonly Color LightColor = new(1f, 0.7f, 0.62f);

	private static readonly Color CoronaColor = new(1f, 0.12f, 0.025f);

	public static Node3D CreateVisual(Vector3 gridCenter, float chamberRadius)
	{
		var radius = chamberRadius * RadiusScale;
		var root = new Node3D
		{
			Name = "RedDwarfSun",
			Position = gridCenter - LightDirection * chamberRadius * DistanceScale,
		};

		root.AddChild(SunVisual.Create(radius, CreatePalette()));
		return root;
	}

	private static MeshInstance3D CreateSurface(float radius, Texture2D texture) =>
		new()
		{
			Name = "Surface",
			Mesh = new SphereMesh
			{
				Radius = radius,
				Height = radius * 2f,
				RadialSegments = 64,
				Rings = 32,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateSurfaceMaterial(texture),
		};

	private static ShaderMaterial CreateSurfaceMaterial(Texture2D texture)
	{
		var shader = GD.Load<Shader>(SurfaceShaderPath)
			?? throw new InvalidOperationException(
				$"Could not load battle red dwarf shader '{SurfaceShaderPath}'.");
		var material = new ShaderMaterial { Shader = shader };
		material.SetShaderParameter("surface_texture", texture);
		return material;
	}

	private static MeshInstance3D CreateCorona(float radius) =>
		StarCorona.Create(
			radius,
			CoronaColor,
			brightness: 0.2f,
			width: 0.18f,
			speed: 0.025f,
			seed: 37f,
			irregularity: 0.82f);


	/// <summary>
	/// A red dwarf is a cooler, redder star than the Sun, so the shared sun
	/// shader is driven with a red dwarf palette instead of the solar one.
	/// </summary>
	private static SunPalette CreatePalette() =>
		new()
		{
			CoreColor = new Color(1.00f, 0.66f, 0.34f),
			MidColor = new Color(1.00f, 0.30f, 0.10f),
			LimbColor = new Color(0.80f, 0.22f, 0.05f),
			ProminenceColor = new Color(1.00f, 0.20f, 0.06f),
			FootpointColor = new Color(1.00f, 0.38f, 0.10f),
			LimbRingColor = new Color(1.00f, 0.24f, 0.07f),
			CoronaColor = new Color(1.00f, 0.26f, 0.09f),
			InnerCoronaColor = new Color(1.00f, 0.40f, 0.16f),
			EmissionStrength = 0.62f,
			LimbDarkening = 0.30f,
			LimbDarkeningSquared = 0.08f,
			LimbEdgeStrength = 0.55f,
			LimbEdgeFloor = 0.40f,
			GranulationCells = 90f,
			GranulationStrength = 0.70f,
			PlageCells = 11f,
			PlageStrength = 0.45f,
			RimColor = new Color(1.00f, 0.32f, 0.10f),
			RimStrength = 0.18f,
			ProminenceCount = 4f,
			ProminenceHeight = 0.16f,
			ProminenceStrength = 0.20f,
			CoronaStrength = 0.09f,
			InnerCoronaStrength = 0.30f,
			LightEnergy = 3.0f,
		};

	public static void Configure(DirectionalLight3D light, Vector3 gridCenter, float chamberRadius)
	{
		light.GlobalPosition = gridCenter;
		light.LookAt(gridCenter + LightDirection, Vector3.Up);
		light.LightColor = LightColor;
		light.LightCullMask = PresentationLayers.World;
		light.LightEnergy = 1.15f;
		light.ShadowEnabled = true;
		light.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits;
		light.DirectionalShadowMaxDistance = chamberRadius * 2.2f;
		light.ShadowBias = 0.08f;
		light.ShadowNormalBias = 1.6f;
		light.ShadowBlur = 1.2f;
	}
}
