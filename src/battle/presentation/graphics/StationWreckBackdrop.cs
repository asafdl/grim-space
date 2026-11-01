using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// The storage station as a broken derelict: the whole structure cut into three jagged sections
/// that have drifted apart across two unequal, oblique ruptures, with exposed framework at
/// the torn ends and sparse wreck fragments trailing outward. It sits
/// in the opening camera's view on the far side of the sky from the sun, and is recolored dark
/// slate so it separates from the rust-colored asteroids under the red sunlight. It is sized in
/// world units (about 45 degrees of the sky) and parked well outside the grid, nearer than the
/// sun, which reads as very far away. A layer of wreck plates fills the space between.
/// </summary>
public static class StationWreckBackdrop
{
	private const string ShaderPath = "res://assets/shaders/station_wreck.gdshader";
	private const ulong SeedSalt = 0x9E3779B97F4A7C15;

	private static readonly string[] FragmentFiles =
	[
		"bent_armor.glb",
		"broken_fuselage.glb",
		"engine_fragment.glb",
		"exposed_truss.glb",
		"hull_panel.glb",
		"severed_prow.glb",
	];

	private static readonly Color HullTint = new(0.42f, 0.44f, 0.5f);
	private static readonly Color FragmentTint = new(0.36f, 0.37f, 0.42f);

	private const float DistanceScale = 5f;
	private const float LengthScale = 4.3f;
	private const float ViewPitchDegrees = 6f;
	private const int DirectionSamples = 600;
	private const float LengthYawDegrees = 28f;
	private const float LengthPitchDegrees = 14f;
	private const float MinSunAngleDegrees = 95f;
	private const float MaxSunAngleDegrees = 140f;
	private const float MaxElevation = 0.8f;
	private const float FirstGapCenter = -0.28f;
	private const float SecondGapCenter = 0.2f;
	private const float GapCenterJitter = 0.025f;
	private const float FirstGapWidth = 0.18f;
	private const float SecondGapWidth = 0.24f;
	private const int GapDebrisCount = 3;
	private const int HaloDebrisCount = 5;
	private const int MidgroundDebrisCount = 6;
	private const float MidgroundMinDistanceScale = 1.5f;
	private const float MidgroundMaxDistanceScale = 3.4f;
	private const float MidgroundSpreadDegrees = 55f;
	private const float MidgroundMinSize = 8f;
	private const float MidgroundMaxSize = 34f;
	private const float MidgroundMinSunAngleDegrees = 25f;
	private const float DebrisMinSize = 0.03f;
	private const float DebrisMaxSize = 0.12f;

	private readonly record struct Section(
		float Min, float Max, Vector2 MinSlope, Vector2 MaxSlope, Vector3 Drift, Vector3 Tilt);

	public static Node3D Create(Vector3 gridCenter, float chamberRadius, int seed, Camera3D camera)
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)(uint)seed ^ SeedSalt };
		var shader = GD.Load<Shader>(ShaderPath)
			?? throw new InvalidOperationException($"Could not load station wreck shader '{ShaderPath}'.");

		var distance = chamberRadius * DistanceScale;
		var length = chamberRadius * LengthScale;
		var stationDirection = PickDirection(rng, camera);
		var station = new Node3D
		{
			Name = "RuinedStation",
			Position = gridCenter + stationDirection * distance,
			Quaternion = PickOrientation(rng, camera),
			Scale = Vector3.One * length,
		};

		var firstGap = FirstGapCenter + rng.RandfRange(-GapCenterJitter, GapCenterJitter);
		var secondGap = SecondGapCenter + rng.RandfRange(-GapCenterJitter, GapCenterJitter);
		var firstSlope = new Vector2(0.35f, -0.65f);
		var secondSlope = new Vector2(-0.55f, -0.25f);
		var sections = new[]
		{
			new Section(-10f, firstGap - FirstGapWidth * 0.5f, Vector2.Zero, firstSlope,
				new Vector3(-0.04f, 0.025f, -0.035f), new Vector3(13f, -9f, 18f)),
			new Section(firstGap + FirstGapWidth * 0.5f, secondGap - SecondGapWidth * 0.5f, firstSlope, secondSlope,
				new Vector3(0.015f, -0.015f, 0f), new Vector3(-5f, 4f, -8f)),
			new Section(secondGap + SecondGapWidth * 0.5f, 10f, secondSlope, Vector2.Zero,
				new Vector3(0.035f, -0.045f, 0.055f), new Vector3(-19f, 16f, -26f)),
		};
		foreach (var section in sections)
			station.AddChild(CreateSection(shader, section, rng));

		var fragmentMaterial = CreateMaterial(shader, null, FragmentTint);
		var radius = Mathf.Max(StationMesh.Size.X, StationMesh.Size.Y) * 0.5f;
		foreach (var (gap, width) in new[] { (firstGap, FirstGapWidth), (secondGap, SecondGapWidth) })
		{
			for (var i = 0; i < GapDebrisCount; i++)
				AddDebris(station, rng, fragmentMaterial, gap + rng.RandfRange(-width * 0.5f, width * 0.5f), radius * rng.RandfRange(0.15f, 1f));
		}

		for (var i = 0; i < HaloDebrisCount; i++)
			AddDebris(station, rng, fragmentMaterial, rng.RandfRange(-0.75f, 0.75f), radius * rng.RandfRange(0.9f, 2.2f));

		var graveyard = new Node3D { Name = "StationGraveyard" };
		graveyard.AddChild(station);
		graveyard.AddChild(CreateMidgroundDebris(rng, gridCenter, chamberRadius, stationDirection, fragmentMaterial));
		return graveyard;
	}

	/// <summary>
	/// Wreck plates drifting between the battlefield and the station, so the fight reads as
	/// happening inside the graveyard and the station gets a nearer size reference.
	/// </summary>
	private static Node3D CreateMidgroundDebris(
		RandomNumberGenerator rng,
		Vector3 gridCenter,
		float chamberRadius,
		Vector3 stationDirection,
		ShaderMaterial material)
	{
		var field = new Node3D { Name = "MidgroundDebris" };
		var placed = 0;
		for (var attempt = 0; placed < MidgroundDebrisCount && attempt < MidgroundDebrisCount * 10; attempt++)
		{
			var axis = stationDirection.Cross(SpaceBackdrop.RandomUnit(rng));
			if (axis.LengthSquared() < 1e-4f)
				continue;

			var spread = Mathf.Sqrt(rng.Randf()) * MidgroundSpreadDegrees;
			var direction = stationDirection.Rotated(axis.Normalized(), Mathf.DegToRad(spread));
			if (Mathf.RadToDeg(direction.AngleTo(-RedDwarfSun.LightDirection)) < MidgroundMinSunAngleDegrees)
				continue;

			var fragment = CreateFragment(
				FragmentFiles[rng.RandiRange(0, FragmentFiles.Length - 1)],
				material,
				rng.RandfRange(MidgroundMinSize, MidgroundMaxSize));
			fragment.Position = gridCenter + direction * chamberRadius
				* rng.RandfRange(MidgroundMinDistanceScale, MidgroundMaxDistanceScale);
			fragment.Quaternion = new Quaternion(SpaceBackdrop.RandomUnit(rng), rng.RandfRange(0f, Mathf.Tau));
			field.AddChild(fragment);
			placed++;
		}

		return field;
	}

	/// <summary>
	/// The allowed direction closest to where the opening camera looks, so the station is in view
	/// at battle start. Allowed directions are on the far side of the sky from the sun (as the planet
	/// is), so the station is mostly sun-lit from the camera with a dark limb, never a silhouette
	/// against the sun like the carrier.
	/// </summary>
	private static Vector3 PickDirection(RandomNumberGenerator rng, Camera3D camera)
	{
		var forward = (-camera.GlobalBasis.Z).Rotated(camera.GlobalBasis.X, Mathf.DegToRad(ViewPitchDegrees));
		var best = Vector3.Zero;
		var bestDot = -2f;
		for (var i = 0; i < DirectionSamples; i++)
		{
			var candidate = SpaceBackdrop.RandomUnit(rng);
			if (!IsAllowedDirection(candidate) || candidate.Dot(forward) <= bestDot)
				continue;
			best = candidate;
			bestDot = candidate.Dot(forward);
		}

		return best;
	}

	private static bool IsAllowedDirection(Vector3 direction)
	{
		var sunAngle = Mathf.RadToDeg(direction.AngleTo(-RedDwarfSun.LightDirection));
		return sunAngle >= MinSunAngleDegrees
			&& sunAngle <= MaxSunAngleDegrees
			&& Mathf.Abs(direction.Y) <= MaxElevation;
	}

	/// <summary>
	/// Lays the station's length across the camera's view, turned three-quarters toward it and
	/// rolled randomly so different battles show different faces.
	/// </summary>
	private static Quaternion PickOrientation(RandomNumberGenerator rng, Camera3D camera)
	{
		var basis = camera.GlobalBasis;
		var side = rng.Randf() < 0.5f ? -1f : 1f;
		var lengthAxis = basis.X
			.Rotated(basis.Y, Mathf.DegToRad(LengthYawDegrees * side))
			.Rotated(basis.Z, Mathf.DegToRad(LengthPitchDegrees));
		var roll = new Basis(Vector3.Back, rng.RandfRange(0f, Mathf.Tau));
		return (Basis.LookingAt(lengthAxis, basis.Y) * roll).GetRotationQuaternion();
	}

	private static Node3D CreateSection(Shader shader, Section section, RandomNumberGenerator rng)
	{
		var center = (Mathf.Max(section.Min, -0.5f) + Mathf.Min(section.Max, 0.5f)) * 0.5f;
		var edgePhase = new Vector2(rng.Randf(), rng.Randf()) * 40f;
		var sourceHull = StationMesh.HullMaterial;
		var hullMaterial = CreateSectionMaterial(shader, section, edgePhase, sourceHull.AlbedoTexture);
		if (sourceHull.AOEnabled && sourceHull.AOTexture is { } occlusion)
		{
			hullMaterial.SetShaderParameter("use_occlusion_texture", true);
			hullMaterial.SetShaderParameter("occlusion_texture", occlusion);
			hullMaterial.SetShaderParameter("occlusion_channel", (int)sourceHull.AOTextureChannel);
		}
		if (sourceHull.RoughnessTexture is { } roughness)
		{
			hullMaterial.SetShaderParameter("use_roughness_texture", true);
			hullMaterial.SetShaderParameter("roughness_texture", roughness);
			hullMaterial.SetShaderParameter("roughness_channel", (int)sourceHull.RoughnessTextureChannel);
		}

		var pivot = new Node3D
		{
			Name = "StationSection",
			Position = new Vector3(0f, 0f, center) + section.Drift,
			RotationDegrees = section.Tilt,
		};
		var hull = new MeshInstance3D
		{
			Name = "Hull",
			Mesh = StationMesh.Hull,
			Position = new Vector3(0f, 0f, -center),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		hull.SetSurfaceOverrideMaterial(StationMesh.HullSurface, hullMaterial);
		for (var i = 0; i < StationMesh.LightMaterials.Count; i++)
		{
			var sourceLight = StationMesh.LightMaterials[i];
			var lightsMaterial = CreateSectionMaterial(shader, section, edgePhase, null);
			lightsMaterial.SetShaderParameter("lights_mode", true);
			// Untextured emissive sheets stay dead; they must not become broad glowing hull panels.
			if (sourceLight.EmissionTexture is { } windows)
			{
				lightsMaterial.SetShaderParameter("use_window_texture", true);
				lightsMaterial.SetShaderParameter("window_texture", windows);
				lightsMaterial.SetShaderParameter("window_color", sourceLight.Emission);
			}
			hull.SetSurfaceOverrideMaterial(i + 1, lightsMaterial);
		}
		PresentationLayers.MarkWorld(hull);
		pivot.AddChild(hull);
		var frameworkMaterial = CreateMaterial(shader, null, FragmentTint);
		if (section.Min > -0.5f)
			AddTornFramework(pivot, rng, frameworkMaterial, section.Min - center, section.MinSlope, -1f);
		if (section.Max < 0.5f)
			AddTornFramework(pivot, rng, frameworkMaterial, section.Max - center, section.MaxSlope, 1f);
		return pivot;
	}

	private static void AddTornFramework(
		Node3D pivot, RandomNumberGenerator rng, ShaderMaterial material, float edge, Vector2 slope, float outward)
	{
		var size = StationMesh.Size;
		for (var i = 0; i < 4; i++)
		{
			var x = size.X * rng.RandfRange(-0.3f, 0.3f);
			var y = size.Y * rng.RandfRange(-0.3f, 0.3f);
			var fragment = CreateFragment(i == 3 ? "bent_armor.glb" : "exposed_truss.glb",
				material, rng.RandfRange(0.08f, 0.15f));
			fragment.Name = "TornFramework";
			fragment.Position = new Vector3(x, y, edge + slope.Dot(new Vector2(x, y)) - outward * 0.025f);
			fragment.RotationDegrees = new Vector3(
				rng.RandfRange(-35f, 35f), rng.RandfRange(-35f, 35f), rng.RandfRange(-70f, 70f));
			pivot.AddChild(fragment);
		}
	}

	private static void AddDebris(
		Node3D root,
		RandomNumberGenerator rng,
		ShaderMaterial material,
		float z,
		float radius)
	{
		var angle = rng.RandfRange(0f, Mathf.Tau);
		var fragment = CreateFragment(
			FragmentFiles[rng.RandiRange(0, FragmentFiles.Length - 1)],
			material,
			rng.RandfRange(DebrisMinSize, DebrisMaxSize));
		fragment.Position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z);
		fragment.Quaternion = new Quaternion(SpaceBackdrop.RandomUnit(rng), rng.RandfRange(0f, Mathf.Tau));
		root.AddChild(fragment);
	}

	private static Node3D CreateFragment(string fileName, ShaderMaterial material, float size)
	{
		var fragment = RuinedCarrierBackdrop.InstantiateFragment(fileName);
		var parts = fragment.FindChildren("*", "MeshInstance3D", true, false)
			.OfType<MeshInstance3D>()
			.ToArray();
		var bounds = default(Aabb);
		for (var i = 0; i < parts.Length; i++)
		{
			var mesh = parts[i].Mesh
				?? throw new InvalidOperationException($"Wreck fragment '{fileName}' has a part without a mesh.");
			bounds = i == 0 ? mesh.GetAabb() : bounds.Merge(mesh.GetAabb());
			parts[i].MaterialOverride = material;
			parts[i].CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			PresentationLayers.MarkWorld(parts[i]);
		}

		var extent = Mathf.Max(bounds.Size.X, Mathf.Max(bounds.Size.Y, bounds.Size.Z));
		if (extent <= 0f)
			throw new InvalidOperationException($"Wreck fragment '{fileName}' has no extent.");

		fragment.Position = -bounds.GetCenter();
		var holder = new Node3D { Name = "WreckFragment", Scale = Vector3.One * (size / extent) };
		holder.AddChild(fragment);
		return holder;
	}

	private static ShaderMaterial CreateSectionMaterial(
		Shader shader,
		Section section,
		Vector2 edgePhase,
		Texture2D? texture)
	{
		var material = CreateMaterial(shader, texture, HullTint);
		material.SetShaderParameter("keep_min", section.Min);
		material.SetShaderParameter("keep_max", section.Max);
		material.SetShaderParameter("keep_min_slope", section.MinSlope);
		material.SetShaderParameter("keep_max_slope", section.MaxSlope);
		material.SetShaderParameter("jag_amplitude", 0.1f);
		material.SetShaderParameter("noise_scale", 18f);
		material.SetShaderParameter("edge_phase", edgePhase);
		return material;
	}

	private static ShaderMaterial CreateMaterial(Shader shader, Texture2D? texture, Color tint)
	{
		var material = new ShaderMaterial { Shader = shader };
		material.SetShaderParameter("use_texture", texture is not null);
		if (texture is not null)
			material.SetShaderParameter("albedo_texture", texture);
		material.SetShaderParameter("tint", tint);
		material.SetShaderParameter("sun_direction", -RedDwarfSun.LightDirection);
		material.SetShaderParameter("planet_direction", RedDwarfSun.LightDirection);
		return material;
	}
}
