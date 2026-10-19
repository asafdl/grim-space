using Godot;

namespace GrimSpace.Presentation.Graphics;

/// <summary>
/// A layered star: photosphere + prominences + corona.
///
/// The three layers are separate draw calls so the additive layers can be
/// tuned independently of the opaque surface.  All of them are procedural -
/// no bitmap star textures are involved - so the look is driven by the
/// measured reference palette rather than by whatever texture happened to be in
/// the repository.  See <c>SUN_NOTES.md</c> for the reference analysis.
/// </summary>
public static class SunVisual
{
	private const string SurfaceShaderPath = "res://assets/shaders/sun_surface.gdshader";
	private const string ProminenceShaderPath = "res://assets/shaders/sun_prominences.gdshader";
	private const string CoronaShaderPath = "res://assets/shaders/sun_corona.gdshader";

	public static Node3D Create(float radius, SunPalette palette)
	{
		if (radius <= 0f)
			throw new System.ArgumentOutOfRangeException(nameof(radius));

		var root = new Node3D { Name = "Sun" };
		root.AddChild(CreateSurface(radius, palette));
		root.AddChild(CreateProminences(radius, palette));
		root.AddChild(CreateCorona(radius, palette));
		return root;
	}

	private static MeshInstance3D CreateSurface(float radius, SunPalette palette)
	{
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(SurfaceShaderPath),
		};
		material.SetShaderParameter("core_color", palette.CoreColor);
		material.SetShaderParameter("mid_color", palette.MidColor);
		material.SetShaderParameter("limb_color", palette.LimbColor);
		material.SetShaderParameter("emission_strength", palette.EmissionStrength);
		material.SetShaderParameter("limb_darkening", palette.LimbDarkening);
		material.SetShaderParameter("limb_darkening_sq", palette.LimbDarkeningSquared);
		material.SetShaderParameter("limb_edge_strength", palette.LimbEdgeStrength);
		material.SetShaderParameter("granulation_cells", palette.GranulationCells);
		material.SetShaderParameter("granulation_strength", palette.GranulationStrength);
		material.SetShaderParameter("plage_cells", palette.PlageCells);
		material.SetShaderParameter("plage_strength", palette.PlageStrength);
		material.SetShaderParameter("rim_color", palette.RimColor);
		material.SetShaderParameter("rim_strength", palette.RimStrength);
		material.SetShaderParameter("sun_radius", radius);
		material.SetShaderParameter("seed", palette.Seed);
		material.SetShaderParameter("debug_term", palette.SurfaceDebug);

		return new MeshInstance3D
		{
			Name = "Surface",
			Mesh = new SphereMesh
			{
				Radius = radius,
				Height = radius * 2f,
				RadialSegments = 96,
				Rings = 48,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = material,
		};
	}

	private static MeshInstance3D CreateProminences(float radius, SunPalette palette)
	{
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(ProminenceShaderPath),
		};
		material.SetShaderParameter("prominence_color", palette.ProminenceColor);
		material.SetShaderParameter("footpoint_color", palette.FootpointColor);
		material.SetShaderParameter("limb_ring_color", palette.LimbRingColor);
		material.SetShaderParameter("brightness", palette.ProminenceStrength);
		material.SetShaderParameter("site_count", Mathf.RoundToInt(palette.ProminenceCount));
		material.SetShaderParameter("loop_height", palette.ProminenceHeight);
		material.SetShaderParameter("seed", palette.Seed);
		material.SetShaderParameter("sun_radius", radius);
		material.SetShaderParameter("quad_half_width", radius * palette.ProminenceQuadWidth * 0.5f);
		material.SetShaderParameter("debug_term", palette.ProminenceDebug);

		return CreateBillboardQuad(
			"Prominences",
			radius * palette.ProminenceQuadWidth,
			radius,
			material);
	}

	private static MeshInstance3D CreateCorona(float radius, SunPalette palette)
	{
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(CoronaShaderPath),
		};
		material.SetShaderParameter("corona_color", palette.CoronaColor);
		material.SetShaderParameter("brightness", palette.CoronaStrength);
		material.SetShaderParameter("seed", palette.Seed);
		material.SetShaderParameter("sun_radius", radius);
		material.SetShaderParameter("quad_half_width", radius * palette.CoronaQuadWidth * 0.5f);

		return CreateBillboardQuad(
			"Corona",
			radius * palette.CoronaQuadWidth,
			radius,
			material);
	}

	/// <summary>
	/// Camera facing quad used by the additive layers.  The shader re-orients
	/// the quad in its vertex stage, so the mesh itself stays an axis aligned
	/// plane; the custom AABB keeps the whole billboard inside the culling
	/// volume.
	/// </summary>
	private static MeshInstance3D CreateBillboardQuad(
		string name,
		float width,
		float radius,
		Material material)
	{
		var half = width * 0.5f;
		return new MeshInstance3D
		{
			Name = name,
			Mesh = new QuadMesh { Size = Vector2.One * width },
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			CustomAabb = new Aabb(
				Vector3.One * -half,
				Vector3.One * width),
			ExtraCullMargin = radius,
		};
	}
}
