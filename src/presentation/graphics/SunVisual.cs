using Godot;

namespace GrimSpace.Presentation.Graphics;

/// <summary>
/// A layered star: photosphere + prominences + corona, plus the light it
/// throws into the world.
///
/// The three draw layers are separate so the additive ones can be tuned
/// independently of the opaque surface.  All of them are procedural apart from
/// the photosphere, which is the real NASA solar surface; the procedural field
/// drifts over it so it never looks like a still image.
///
/// The star is also a light source: a point light with a falloff range sits at
/// its centre, so anything near it is lit by it and falls off with distance.
/// </summary>
public static class SunVisual
{
	private const string SurfaceShaderPath = "res://assets/shaders/sun_surface.gdshader";
	private const string ProminenceShaderPath = "res://assets/shaders/sun_prominences.gdshader";
	private const string CoronaShaderPath = "res://assets/shaders/sun_corona.gdshader";
	private const string PhotospherePath = "res://assets/textures/8k_sun.jpg";
	private const string PhotosphereFallbackPath = "res://assets/textures/2k_sun.jpg";

	/// <summary>Width of the prominence billboard, as a multiple of the star radius.</summary>
	private const float ProminenceQuadWidth = 10f;

	/// <summary>Radius of the corona shell, as a multiple of the star radius.</summary>
	private const float CoronaShellRadius = 2.2f;

	private static ShaderMaterial? _surfaceMaterial;
	private static ShaderMaterial? _prominenceMaterial;

	private static SunProminenceCasting.ProminenceTurn[]? _prominenceSchedule;
	private static float _prominenceCastingScale = 1f;

	/// <summary>
	/// Sets how long one timeline design second lasts on the casting clock
	/// (1 = design speed, as filmed by the pipeline's own scripts).  The
	/// strategic map plays its arcs at minutes scale (see
	/// <see cref="SunProminenceCasting.GrowthSecondsAt1x"/>); the sun test
	/// harness keeps design speed.
	/// </summary>
	public static void SetProminenceCastingScale(float castingSecondsPerTimelineSecond) =>
		_prominenceCastingScale = Mathf.Max(castingSecondsPerTimelineSecond, 0.0001f);

	/// <summary>
	/// Installs the timeline's prominence casting schedule
	/// (<c>sun_events.json</c>'s <c>prominence_roles</c>, the pipeline's
	/// authority).  While set, <see cref="UpdatePhenomena"/> drives the
	/// shader's one role from it; consumers without a schedule leave the
	/// material's role uniforms at their defaults, which the shader reads as
	/// self-drive (the same one-role law, cycle-internal).
	/// </summary>
	public static void SetProminenceSchedule(IEnumerable<(int Holder, float Start, float Death)> turns)
	{
		_prominenceSchedule = [.. turns
			.OrderBy(turn => turn.Start)
			.Select(turn => new SunProminenceCasting.ProminenceTurn(turn.Holder, turn.Start, turn.Death))];
	}

	/// <summary>
	/// Pushes the current state of the transient phenomena (sunspots, flare,
	/// coronal rain, CME) into the star's materials, plus the current
	/// prominence role from the casting office — one loop at a time, by the
	/// timeline's seeded roll (<see cref="SunProminenceCasting"/>) — at the
	/// star's own casting clock <paramref name="promotionTime"/>, in seconds.
	/// </summary>
	public static void UpdatePhenomena(SunPhenomena phenomena, float promotionTime)
	{
		if (_surfaceMaterial is not null)
		{
			_surfaceMaterial.SetShaderParameter("spots", phenomena.Spots);
			_surfaceMaterial.SetShaderParameter("flare_data", phenomena.Flare);
		}
		if (_prominenceMaterial is not null && _prominenceSchedule is not null)
		{
			var (holder, gain) = SunProminenceCasting.Resolve(
				_prominenceSchedule,
				promotionTime,
				_prominenceCastingScale);
			_prominenceMaterial.SetShaderParameter("role_site", (float)holder);
			_prominenceMaterial.SetShaderParameter("role_gain", gain);
		}
		if (_prominenceMaterial is not null)
		{
			_prominenceMaterial.SetShaderParameter("rain_amount", phenomena.Rain);
			_prominenceMaterial.SetShaderParameter("cme_data", phenomena.Cme);
		}
	}

	public static Node3D Create(float radius, SunPalette palette)
	{
		if (radius <= 0f)
			throw new System.ArgumentOutOfRangeException(nameof(radius));

		var root = new Node3D { Name = "Sun" };
		root.AddChild(CreateSurface(radius, palette));
		root.AddChild(CreateProminences(radius, palette));
		root.AddChild(CreateCorona(radius, palette));
		if (palette.LightEnergy > 0f)
			root.AddChild(CreateLight(radius, palette));
		return root;
	}

	/// <summary>
	/// Creates the screen space glare for a star.  The caller owns it: it is a
	/// CanvasLayer, so it must live outside the 3D tree.
	/// </summary>
	public static SunGlareOverlay CreateGlareOverlay(Node3D target, float radius) =>
		new()
		{
			Target = target,
			TargetRadius = radius,
		};

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
		material.SetShaderParameter("limb_edge_floor", palette.LimbEdgeFloor);
		material.SetShaderParameter("granulation_cells", palette.GranulationCells);
		material.SetShaderParameter("granulation_strength", palette.GranulationStrength);
		material.SetShaderParameter("plage_cells", palette.PlageCells);
		material.SetShaderParameter("plage_strength", palette.PlageStrength);
		material.SetShaderParameter("supergranulation_cells", palette.SupergranulationCells);
		material.SetShaderParameter("supergranulation_strength", palette.SupergranulationStrength);
		material.SetShaderParameter("rim_color", palette.RimColor);
		material.SetShaderParameter("rim_strength", palette.RimStrength);
		material.SetShaderParameter("core_burn", palette.CoreBurn);
		material.SetShaderParameter("texture_blend", palette.TextureBlend);
		material.SetShaderParameter("texture_tiling", palette.TextureTiling);
		material.SetShaderParameter("cellular_cells", palette.CellularCells);
		material.SetShaderParameter("cellular_strength", palette.CellularStrength);
		material.SetShaderParameter("lane_width", palette.LaneWidth);
		material.SetShaderParameter("drift_strength", palette.DriftStrength);
		material.SetShaderParameter("sun_radius", radius);
		material.SetShaderParameter("seed", palette.Seed);
		material.SetShaderParameter("debug_term", palette.SurfaceDebug);
		material.SetShaderParameter("photosphere", LoadPhotosphere());
		_surfaceMaterial = material;

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

	private static Texture2D LoadPhotosphere()
	{
		if (ResourceLoader.Exists(PhotospherePath))
			return GD.Load<Texture2D>(PhotospherePath);
		return GD.Load<Texture2D>(PhotosphereFallbackPath);
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
		material.SetShaderParameter("loop_height", palette.ProminenceHeight);
		material.SetShaderParameter("seed", palette.Seed);
		material.SetShaderParameter("sun_radius", radius);
		material.SetShaderParameter("quad_half_width", radius * palette.ProminenceQuadWidth * 0.5f);
		material.SetShaderParameter("debug_term", palette.ProminenceDebug);
		material.SetShaderParameter("filament_width", palette.ProminenceFilamentWidth);
		material.SetShaderParameter("loop_span", palette.ProminenceLoopSpan);
		material.SetShaderParameter("limb_ring_strength", palette.LimbRingStrength);
		material.SetShaderParameter("spicule_haze", palette.SpiculeHaze);
		// Role defaults: negative = self-drive.  The shader then runs the same
		// one-role law cycle-internally (a seeded roll of 0..3 done in-shader);
		// a consumer that installs the timeline's casting schedule drives the
		// role per frame instead (see SetProminenceSchedule).
		material.SetShaderParameter("role_site", -1f);
		material.SetShaderParameter("role_gain", -1f);
		_prominenceMaterial = material;

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
		material.SetShaderParameter("inner_color", palette.InnerCoronaColor);
		material.SetShaderParameter("brightness", palette.CoronaStrength);
		material.SetShaderParameter("inner_brightness", palette.InnerCoronaStrength);
		material.SetShaderParameter("streamer_strength", palette.CoronaStreamerStrength);
		material.SetShaderParameter("halo_reach", palette.CoronaHaloReach);
		material.SetShaderParameter("seed", palette.Seed);
		material.SetShaderParameter("sun_radius", radius);
		material.SetShaderParameter("shell_radius", radius * palette.CoronaShellRadius);

		// A concentric shell, not a billboard: a shell cannot show an edge.
		return new MeshInstance3D
		{
			Name = "Corona",
			Mesh = new SphereMesh
			{
				Radius = radius * palette.CoronaShellRadius,
				Height = radius * palette.CoronaShellRadius * 2f,
				RadialSegments = 48,
				Rings = 24,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = material,
			ExtraCullMargin = radius * palette.CoronaShellRadius,
		};
	}

	/// <summary>
	/// The star as a real light source: a warm point light with a falloff
	/// range, so nearby ships and rocks are lit by the star and the light
	/// falls off with distance.
	/// </summary>
	private static OmniLight3D CreateLight(float radius, SunPalette palette) =>
		new()
		{
			Name = "Light",
			LightColor = palette.LightColor,
			LightEnergy = palette.LightEnergy,
			OmniRange = radius * palette.LightRange,
			LightSpecular = 0.25f,
			ShadowEnabled = false,
		};

	/// <summary>
	/// Camera facing quad used by the prominence layer.  The shader re-orients
	/// the quad in its vertex stage, so the mesh itself stays an axis aligned
	/// plane; the custom AABB keeps the whole billboard inside the culling
	/// volume.  It is deliberately large so its own edges can never fall
	/// inside the frame.
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
