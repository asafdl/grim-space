using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class RedDwarfSun
{
	private const string SurfaceTexturePath = "res://assets/textures/2k_sun.jpg";
	private const float DistanceScale = 2.4f;
	private const float RadiusScale = 0.72f;

	public static readonly Vector3 LightDirection = new Vector3(-0.58f, -0.22f, -0.78f).Normalized();
	public static readonly Color LightColor = new(1f, 0.7f, 0.62f);

	private static readonly Color SurfaceTint = new(1f, 0.3f, 0.22f);
	private static readonly Color CoronaColor = new(1f, 0.08f, 0.025f);

	public static Node3D CreateVisual(Vector3 gridCenter, float chamberRadius)
	{
		var radius = chamberRadius * RadiusScale;
		var root = new Node3D
		{
			Name = "RedDwarfSun",
			Position = gridCenter - LightDirection * chamberRadius * DistanceScale,
		};

		var surfaceTexture = GD.Load<Texture2D>(SurfaceTexturePath);
		root.AddChild(CreateSurface(radius, surfaceTexture));
		root.AddChild(CreateCorona(radius));
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
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = SurfaceTint,
				AlbedoTexture = texture,
				EmissionEnabled = true,
				Emission = SurfaceTint,
				EmissionTexture = texture,
				EmissionEnergyMultiplier = 1.35f,
			},
		};

	private static MeshInstance3D CreateCorona(float radius) =>
		new()
		{
			Name = "Corona",
			Mesh = new SphereMesh
			{
				Radius = radius * 1.08f,
				Height = radius * 2.16f,
				RadialSegments = 48,
				Rings = 24,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Add,
				AlbedoColor = CoronaColor with { A = 0.1f },
				EmissionEnabled = true,
				Emission = CoronaColor,
				EmissionEnergyMultiplier = 0.75f,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
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
