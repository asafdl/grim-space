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
