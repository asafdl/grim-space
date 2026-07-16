using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class RedDwarfSun
{
	public static readonly Vector3 LightDirection = new Vector3(-0.58f, -0.22f, -0.78f).Normalized();
	public static readonly Color LightColor = new(1f, 0.4f, 0.2f);
	public static readonly Color SunCoreColor = new(1f, 0.34f, 0.1f);
	public static readonly Color SunHaloColor = new(1f, 0.18f, 0.06f);

	public static Node3D CreateVisual(Vector3 gridCenter, float chamberRadius)
	{
		var sunPosition = gridCenter - LightDirection * chamberRadius * 2.4f;
		var root = new Node3D { Name = "RedDwarfSun", Position = sunPosition };

		var coreRadius = chamberRadius * 0.22f;
		root.AddChild(new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = coreRadius, RadialSegments = 24, Rings = 16 },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = SunCoreColor,
				EmissionEnabled = true,
				Emission = SunCoreColor,
				EmissionEnergyMultiplier = 2.4f,
			},
		});

		root.AddChild(new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = coreRadius * 1.55f, RadialSegments = 16, Rings = 12 },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Add,
				AlbedoColor = SunHaloColor with { A = 0.35f },
				EmissionEnabled = true,
				Emission = SunHaloColor,
				EmissionEnergyMultiplier = 1.2f,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			},
		});

		return root;
	}

	public static void Configure(DirectionalLight3D light, Vector3 gridCenter, float chamberRadius)
	{
		light.GlobalPosition = gridCenter;
		light.LookAt(gridCenter + LightDirection, Vector3.Up);
		light.LightColor = LightColor;
		light.LightEnergy = 1.35f;
		light.ShadowEnabled = true;
		light.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits;
		light.DirectionalShadowMaxDistance = chamberRadius * 2.2f;
		light.ShadowBias = 0.08f;
		light.ShadowNormalBias = 1.6f;
		light.ShadowBlur = 1.2f;
	}
}
