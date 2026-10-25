using Godot;
using GrimSpace.Presentation.Graphics;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class RedDwarfSun
{
	private const string SurfaceTexturePath = "res://assets/textures/2k_sun.jpg";
	private const string SurfaceShaderPath =
		"res://assets/shaders/battle_red_dwarf.gdshader";
	public const float DistanceScale = 5f;
	public const float RadiusScale = 0.72f;
	private const float ReferenceDistanceScale = 2.4f;

	public static readonly Vector3 LightDirection = new Vector3(-0.58f, -0.22f, -0.78f).Normalized();
	public static readonly Color LightColor = new(1f, 0.7f, 0.62f);

	private static readonly Color CoronaColor = new(1f, 0.12f, 0.025f);

	public static Vector3 VisualPosition(Vector3 gridCenter, float chamberRadius, Camera3D camera)
	{
		var referencePosition = gridCenter - LightDirection * chamberRadius * ReferenceDistanceScale;
		var viewportSize = camera.GetViewport().GetVisibleRect().Size;
		var screenPosition = VisibleReferencePosition(camera, referencePosition, viewportSize);
		var cameraForward = -camera.GlobalBasis.Z;
		var referenceDepth = (referencePosition - camera.GlobalPosition).Dot(cameraForward);
		var depth = referenceDepth * DistanceScale / ReferenceDistanceScale;
		var halfHeight = depth * Mathf.Tan(Mathf.DegToRad(camera.Fov * 0.5f));
		var screenOffset = new Vector2(
			screenPosition.X / viewportSize.X * 2f - 1f,
			1f - screenPosition.Y / viewportSize.Y * 2f);
		return camera.GlobalPosition
			+ cameraForward * depth
			+ camera.GlobalBasis.X * screenOffset.X * halfHeight * viewportSize.X / viewportSize.Y
			+ camera.GlobalBasis.Y * screenOffset.Y * halfHeight;
	}

	private static Vector2 VisibleReferencePosition(
		Camera3D camera,
		Vector3 referencePosition,
		Vector2 viewportSize)
	{
		const float margin = 150f;
		var screenPosition = camera.UnprojectPosition(referencePosition);
		var safeRect = new Rect2(
			margin,
			margin,
			viewportSize.X - margin * 2f,
			viewportSize.Y - margin * 2f);
		if (safeRect.HasPoint(screenPosition))
			return screenPosition;

		var towardCenter = (viewportSize * 0.5f - screenPosition).Normalized();
		var horizontalEntry = towardCenter.X > 0f
			? (safeRect.Position.X - screenPosition.X) / towardCenter.X
			: towardCenter.X < 0f
				? (safeRect.End.X - screenPosition.X) / towardCenter.X
				: 0f;
		var verticalEntry = towardCenter.Y > 0f
			? (safeRect.Position.Y - screenPosition.Y) / towardCenter.Y
			: towardCenter.Y < 0f
				? (safeRect.End.Y - screenPosition.Y) / towardCenter.Y
				: 0f;
		var distanceToSafeRect = Mathf.Max(horizontalEntry, verticalEntry);
		return screenPosition + towardCenter * Mathf.Max(0f, distanceToSafeRect - 60f);
	}

	public static Node3D CreateVisual(Vector3 gridCenter, float chamberRadius, Camera3D camera)
	{
		var radius = chamberRadius * RadiusScale;
		var root = new Node3D
		{
			Name = "RedDwarfSun",
			Position = VisualPosition(gridCenter, chamberRadius, camera),
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

	public static void Configure(
		DirectionalLight3D light,
		Vector3 gridCenter,
		float chamberRadius,
		Camera3D camera)
	{
		var lightDirection = (gridCenter - VisualPosition(gridCenter, chamberRadius, camera)).Normalized();
		light.GlobalPosition = gridCenter;
		light.LookAt(gridCenter + lightDirection, Vector3.Up);
		light.LightColor = LightColor;
		light.LightCullMask = PresentationLayers.World;
		light.LightEnergy = 2.1f;
		light.ShadowEnabled = true;
		light.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits;
		light.DirectionalShadowMaxDistance = chamberRadius * 2.2f;
		light.ShadowBias = 0.08f;
		light.ShadowNormalBias = 0.6f;
		light.ShadowBlur = 0.7f;
	}
}
