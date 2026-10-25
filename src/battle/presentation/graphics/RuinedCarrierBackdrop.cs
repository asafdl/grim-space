using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class RuinedCarrierBackdrop
{
	private const string WreckPath = "res://assets/models/wreck_debris_pack/";
	private const string ShaderPath = "res://assets/shaders/ruined_carrier.gdshader";
	private const float HullScale = 5f;
	private const float BreakHalfWidth = 1.2f;
	private const float HalfSeparation = 0.55f;
	private const float CarrierLongitudinalCenter = -2.21f;

	public static Node3D Create(Vector3 gridCenter, float chamberRadius, Camera3D camera)
	{
		var sunPosition = RedDwarfSun.VisualPosition(gridCenter, chamberRadius, camera);
		var sunDirection = (sunPosition - camera.GlobalPosition).Normalized();
		var carrierDistance = camera.GlobalPosition.DistanceTo(sunPosition)
			- chamberRadius * RedDwarfSun.RadiusScale
			- HullScale * 1.5f;
		var sunRadius = chamberRadius * RedDwarfSun.RadiusScale;
		var root = new Node3D
		{
			Name = "RuinedCarrier",
			Position = camera.GlobalPosition
				+ sunDirection * carrierDistance
				- camera.GlobalBasis.X * sunRadius * 0.55f
				- camera.GlobalBasis.Y * sunRadius * 0.12f,
			Basis = Basis.LookingAt(camera.GlobalBasis.X, camera.GlobalBasis.Y)
				.Rotated(Vector3.Right, Mathf.DegToRad(18f)),
			Scale = Vector3.One * HullScale,
		};
		root.AddChild(CreateCarrierHalf(
			keepLowerSection: true,
			pivotZ: -8f,
			rotationDegrees: new Vector3(8f, -7f, 11f)));
		root.AddChild(CreateCarrierHalf(
			keepLowerSection: false,
			pivotZ: 4f,
			rotationDegrees: new Vector3(-10f, 6f, -8f)));
		AddBreakDebris(root);
		return root;
	}

	private static Node3D CreateCarrierHalf(
		bool keepLowerSection,
		float pivotZ,
		Vector3 rotationDegrees)
	{
		var direction = keepLowerSection ? -1f : 1f;
		var cutPlane = CarrierLongitudinalCenter + direction * BreakHalfWidth;
		var pivot = new Node3D
		{
			Name = keepLowerSection ? "AftHullPivot" : "ForeHullPivot",
			Position = new Vector3(0f, 0f, pivotZ),
			RotationDegrees = rotationDegrees,
		};
		var half = CarrierMesh.CreateHullInstance();
		half.Name = keepLowerSection ? "AftHull" : "ForeHull";
		half.Scale = Vector3.One;
		half.Position = new Vector3(
			0f,
			0f,
			-pivotZ - CarrierLongitudinalCenter + direction * HalfSeparation);

		var meshParts = half.FindChildren("*", "MeshInstance3D", true, false)
			.OfType<MeshInstance3D>()
			.Prepend(half)
			.ToArray();
		half.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		foreach (var mesh in meshParts)
		{
			var shaderMaterial = new ShaderMaterial
			{
				Shader = GD.Load<Shader>(ShaderPath)
					?? throw new InvalidOperationException($"Could not load ruined-carrier shader '{ShaderPath}'."),
			};
			shaderMaterial.SetShaderParameter("cut_plane", cutPlane);
			shaderMaterial.SetShaderParameter("keep_lower_section", keepLowerSection);
			mesh.MaterialOverride = shaderMaterial;
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}

		pivot.AddChild(half);
		return pivot;
	}

	private static void AddBreakDebris(Node3D root)
	{
		var center = CarrierLongitudinalCenter;
		AddDebris(root, "exposed_truss.glb", new Vector3(0f, 1.2f, center), new Vector3(18f, 23f, -12f), 0.68f);
		AddDebris(root, "broken_fuselage.glb", new Vector3(1.5f, -0.6f, center + 0.3f), new Vector3(-19f, 38f, 21f), 0.58f);
		AddDebris(root, "engine_fragment.glb", new Vector3(-1.8f, 0.4f, center - 0.4f), new Vector3(12f, -27f, 46f), 0.54f);
		AddDebris(root, "bent_armor.glb", new Vector3(0.8f, 2.1f, center - 0.8f), new Vector3(34f, 9f, 62f), 0.5f);
		AddDebris(root, "hull_panel.glb", new Vector3(-2.1f, -1.3f, center + 0.7f), new Vector3(-28f, 14f, -31f), 0.55f);
		AddDebris(root, "severed_prow.glb", new Vector3(2.4f, 0.9f, center + 0.1f), new Vector3(16f, -32f, 18f), 0.46f);
		AddDebris(root, "hull_panel.glb", new Vector3(-0.7f, 2.8f, center + 1.1f), new Vector3(52f, 8f, 39f), 0.38f);
		AddDebris(root, "bent_armor.glb", new Vector3(2.8f, -1.8f, center - 1.2f), new Vector3(-41f, 27f, 13f), 0.4f);
		AddDebris(root, "exposed_truss.glb", new Vector3(-2.6f, 1.4f, center + 1.3f), new Vector3(8f, -48f, 31f), 0.42f);
		AddDebris(root, "engine_fragment.glb", new Vector3(0.5f, -2.5f, center - 1.4f), new Vector3(-22f, 14f, -53f), 0.38f);
		AddDebris(root, "hull_panel.glb", new Vector3(1.2f, 2.7f, center - 0.2f), new Vector3(24f, 61f, -17f), 0.32f);
		AddDebris(root, "broken_fuselage.glb", new Vector3(-1.3f, -2.1f, center + 1.6f), new Vector3(37f, -16f, 49f), 0.34f);
		AddDebris(root, "hull_panel.glb", new Vector3(3.1f, 1.6f, center + 2.1f), new Vector3(-17f, 34f, 56f), 0.28f);
		AddDebris(root, "bent_armor.glb", new Vector3(-3.1f, -0.3f, center - 2.2f), new Vector3(29f, -37f, -12f), 0.27f);
		AddDebris(root, "engine_fragment.glb", new Vector3(0.2f, 3.2f, center + 0.8f), new Vector3(-36f, 22f, 47f), 0.25f);
		AddDebris(root, "hull_panel.glb", new Vector3(-0.4f, -3f, center - 0.6f), new Vector3(44f, 18f, -25f), 0.24f);
		AddDebris(root, "exposed_truss.glb", new Vector3(3.3f, -0.5f, center - 0.3f), new Vector3(-13f, 52f, 26f), 0.22f);
		AddDebris(root, "broken_fuselage.glb", new Vector3(-2.2f, 2.4f, center - 1.8f), new Vector3(31f, 11f, -44f), 0.24f);
	}

	private static void AddDebris(
		Node3D root,
		string fileName,
		Vector3 position,
		Vector3 rotationDegrees,
		float scale)
	{
		var path = WreckPath + fileName;
		var scene = GD.Load<PackedScene>(path)
			?? throw new InvalidOperationException($"Could not load wreck fragment '{path}'.");
		var fragment = scene.Instantiate<Node3D>();
		fragment.Name = Path.GetFileNameWithoutExtension(fileName);
		fragment.Position = position;
		fragment.RotationDegrees = rotationDegrees;
		fragment.Scale = Vector3.One * scale;
		foreach (var mesh in fragment.FindChildren("*", "MeshInstance3D", true, false)
			.OfType<MeshInstance3D>())
		{
			if (mesh.Mesh is not { } meshResource)
				throw new InvalidOperationException($"Wreck fragment '{mesh.Name}' does not have a mesh.");
			for (var surface = 0; surface < meshResource.GetSurfaceCount(); surface++)
			{
				if (meshResource.SurfaceGetMaterial(surface) is not BaseMaterial3D sourceMaterial
					|| sourceMaterial.Duplicate() is not BaseMaterial3D material)
					throw new InvalidOperationException(
						$"Wreck mesh '{mesh.Name}' surface {surface} must use a BaseMaterial3D material.");

				material.AlbedoColor = Colors.Black;
				material.AlbedoTexture = null;
				material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
				material.Roughness = 1f;
				material.Metallic = 0f;
				mesh.SetSurfaceOverrideMaterial(surface, material);
			}
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			mesh.Layers = PresentationLayers.World;
		}
		root.AddChild(fragment);
	}
}
