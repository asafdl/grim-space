using Godot;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>Seeded cloud visuals for cryo plumes and ion storms.</summary>
public partial class CryoPlumeView : Node3D
{
	private const string CloudModelPath = "res://assets/models/spaceobjects/cloud.glb";
	private const string MessierPath = "res://assets/textures/messier_17.jpg";
	internal const string PurpleNebulaPath = "res://assets/textures/purple_nebula_1.png";
	private const string IcePath = "res://assets/textures/ice_water_125.jpg";
	private static readonly Color IceColor = new(0.35f, 0.78f, 1f, 0.55f);
	public int EncounterSeed { private get; init; }
	private Coord[] _ionCells = [];
	private static readonly Dictionary<Mesh, CloudFragmentMesh> FragmentMeshes = [];
	private static PackedScene? _cloudModel;
	private readonly Dictionary<(Mesh Source, int Seed), Mesh> _generatedMeshes = [];
	private static readonly Shader CloudShader = new()
	{
		Code =
			"""
			shader_type spatial;
			render_mode unshaded, cull_disabled, shadows_disabled, depth_draw_never;

			uniform sampler2D albedo_texture : source_color;
			uniform sampler2D ice_texture : source_color, filter_linear, repeat_enable;
			uniform vec4 ice_color : source_color;
			uniform vec3 plume_center;
			uniform float cell_size;
			uniform vec3 noise_offset;
			varying vec3 plume_position;
			varying vec3 plume_normal;

			float hash_cell(vec3 cell) {
				return fract(sin(dot(cell, vec3(127.1, 311.7, 74.7))) * 43758.5453);
			}

			float value_noise(vec3 position) {
				vec3 cell = floor(position);
				vec3 blend = fract(position);
				blend = blend * blend * (3.0 - 2.0 * blend);
				float lower = mix(
					mix(hash_cell(cell), hash_cell(cell + vec3(1.0, 0.0, 0.0)), blend.x),
					mix(hash_cell(cell + vec3(0.0, 1.0, 0.0)), hash_cell(cell + vec3(1.0, 1.0, 0.0)), blend.x),
					blend.y);
				float upper = mix(
					mix(hash_cell(cell + vec3(0.0, 0.0, 1.0)), hash_cell(cell + vec3(1.0, 0.0, 1.0)), blend.x),
					mix(hash_cell(cell + vec3(0.0, 1.0, 1.0)), hash_cell(cell + vec3(1.0, 1.0, 1.0)), blend.x),
					blend.y);
				return mix(lower, upper, blend.z);
			}

			void vertex() {
				plume_position = ((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz - plume_center) / cell_size;
				plume_normal = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz;
			}

			void fragment() {
				float broad = value_noise(plume_position * 4.5 + noise_offset);
				float fine = value_noise(plume_position * 13.0 + noise_offset);
				float density = broad * 0.8 + fine * 0.2;
				float opacity = smoothstep(0.40, 0.64, density);
				ALPHA = texture(albedo_texture, UV).a * ice_color.a * opacity;
				if (ALPHA < 0.025) {
					discard;
				}
				vec3 weights = abs(normalize(plume_normal));
				weights /= weights.x + weights.y + weights.z;
				vec2 offset = noise_offset.xy * 0.01;
				vec3 ice = texture(ice_texture, offset + plume_position.yz * 0.5).rgb * weights.x
					+ texture(ice_texture, offset + plume_position.xz * 0.5).rgb * weights.y
					+ texture(ice_texture, offset + plume_position.xy * 0.5).rgb * weights.z;
				ALBEDO = mix(ice_color.rgb, vec3(1.0), 0.35) * ice * 1.4;
			}
			""",
	};

	internal static readonly Shader IonShader = new()
	{
		Code =
			"""
			shader_type spatial;
			render_mode unshaded, cull_disabled, depth_draw_never, shadows_disabled;

			uniform sampler2D nebula_texture : source_color, filter_linear;
			uniform sampler2D cloud_texture : source_color, filter_linear;
			uniform vec4 cloud_color : source_color;
			uniform vec3 cloud_center;
			uniform float cell_size;
			uniform float opacity = 0.1;
			uniform bool lightning_enabled = true;
			uniform float flash_offset = 0.0;
			uniform float flash_period = 3.5;
			varying vec3 cloud_position;
			varying vec3 cloud_normal;

			void vertex() {
				cloud_position = ((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz - cloud_center) / cell_size;
				cloud_normal = normalize(mat3(MODEL_MATRIX) * NORMAL);
			}

			void fragment() {
				vec3 weights = abs(normalize(cloud_normal));
				weights /= weights.x + weights.y + weights.z;
				vec2 offset = vec2(0.42, 0.42);
				vec3 detail = texture(nebula_texture, offset + cloud_position.yz * 0.3).rgb * weights.x
					+ texture(nebula_texture, offset + cloud_position.xz * 0.3).rgb * weights.y
					+ texture(nebula_texture, offset + cloud_position.xy * 0.3).rgb * weights.z;
				vec4 cloud = texture(cloud_texture, UV) * cloud_color;
				ALBEDO = mix(vec3(0.25, 0.08, 0.32), detail * vec3(1.5, 1.1, 1.8), 0.85);
				ALPHA = cloud.a * opacity;
				if (lightning_enabled) {
					float phase = mod(TIME + flash_offset, flash_period);
					float flash = max(0.0, 1.0 - abs(phase - 0.3) / 0.07)
						+ 0.65 * max(0.0, 1.0 - abs(phase - 0.48) / 0.05);
					float locality = pow(max(0.0, 1.0 - length(cloud_position - vec3(0.2, 0.18, 0.08)) * 1.4), 2.0);
					ALBEDO += vec3(0.6, 0.5, 1.0) * flash * locality * 3.0;
				}
			}
			""",
	};

	public void Build(IEnumerable<CryoPlume> plumes) =>
		BuildClouds(plumes.Select(plume => plume.Center), false);

	public void ApplyIonStorms(IReadOnlyList<Coord> cells)
	{
		if (_ionCells.SequenceEqual(cells))
			return;

		_ionCells = cells.ToArray();
		foreach (var child in GetChildren())
			child.QueueFree();
		BuildClouds(_ionCells, true);
	}

	private void BuildClouds(IEnumerable<Coord> positions, bool ionStorm)
	{
		var cells = positions.ToArray();
		if (cells.Length == 0)
			return;

		Texture2D[] textures = ionStorm
			? [GD.Load<Texture2D>(MessierPath) ?? throw new InvalidOperationException($"Could not load '{MessierPath}'."),
				GD.Load<Texture2D>(PurpleNebulaPath) ?? throw new InvalidOperationException($"Could not load '{PurpleNebulaPath}'.")]
			: [GD.Load<Texture2D>(IcePath) ?? throw new InvalidOperationException($"Could not load '{IcePath}'.")];
		foreach (var cell in cells)
		{
			var seed = CloudPattern.SeedFor(EncounterSeed, cell);
			var rng = new RandomNumberGenerator
			{
				Seed = (ulong)seed,
			};
			var noiseOffset = new Vector3(
				rng.RandfRange(0f, 64f), rng.RandfRange(0f, 64f), rng.RandfRange(0f, 64f));
			var flashPeriod = ionStorm ? rng.RandfRange(2.8f, 5f) : 0f;
			var flashOffset = ionStorm ? rng.RandfRange(0f, flashPeriod) : 0f;
			var cloud = CreateCloud(seed, source =>
			{
				var material = new ShaderMaterial { Shader = ionStorm ? IonShader : CloudShader };
				if (ionStorm)
				{
					material.SetShaderParameter("nebula_texture", textures[seed % textures.Length]);
					material.SetShaderParameter("cloud_texture", source.AlbedoTexture);
					material.SetShaderParameter("cloud_color", source.AlbedoColor);
					material.SetShaderParameter("cloud_center", WorldMapping.ToWorld(cell));
					material.SetShaderParameter("flash_period", flashPeriod);
					material.SetShaderParameter("flash_offset", flashOffset);
				}
				else
				{
					material.SetShaderParameter("albedo_texture", source.AlbedoTexture);
					material.SetShaderParameter("ice_texture", textures[0]);
					material.SetShaderParameter("ice_color", IceColor);
					material.SetShaderParameter("plume_center", WorldMapping.ToWorld(cell));
					material.SetShaderParameter("noise_offset", noiseOffset);
				}
				material.SetShaderParameter("cell_size", WorldMapping.CellSize);
				return material;
			}, _generatedMeshes);
			if (!ionStorm)
			{
				var scene = cloud.GetChild<Node3D>(0);
				var center = -scene.Position;
				scene.RotateY(rng.RandfRange(0f, Mathf.Tau));
				scene.RotateX(rng.RandfRange(-0.3f, 0.3f));
				scene.RotateZ(rng.RandfRange(-0.3f, 0.3f));
				scene.Position = -(scene.Basis * center);
			}
			cloud.Name = ionStorm ? "IonStormCloud" : "CryoPlumeCloud";
			cloud.Position = WorldMapping.ToWorld(cell);
			AddChild(cloud);
		}
	}

	/// <summary>
	/// Builds the shared cloud model cut into seeded fragments, centered on the origin and scaled to one cell.
	/// </summary>
	internal static Node3D CreateCloud(
		int seed,
		Func<StandardMaterial3D, ShaderMaterial> createMaterial,
		Dictionary<(Mesh Source, int Seed), Mesh>? generatedMeshes = null)
	{
		_cloudModel ??= GD.Load<PackedScene>(CloudModelPath)
			?? throw new InvalidOperationException($"Could not load cloud model '{CloudModelPath}'.");
		var regions = CloudPattern.Create(seed);
		var scene = _cloudModel.Instantiate<Node3D>();
		try
		{
			var meshes = scene.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>().ToArray();
			if (meshes.Length == 0)
				throw new InvalidOperationException($"Cloud model '{CloudModelPath}' has no meshes.");

			var bounds = default(Aabb);
			for (var i = 0; i < meshes.Length; i++)
			{
				if (meshes[i].Mesh is not { } mesh)
					throw new InvalidOperationException($"Cloud model '{CloudModelPath}' has an empty mesh.");

				var transform = Transform3D.Identity;
				for (Node3D? node = meshes[i]; node is not null; node = node.GetParent() as Node3D)
				{
					transform = node.Transform * transform;
					if (node == scene)
						break;
				}

				var partBounds = transform * mesh.GetAabb();
				bounds = i == 0 ? partBounds : bounds.Merge(partBounds);
			}

			var extent = Mathf.Max(bounds.Size.X, Mathf.Max(bounds.Size.Y, bounds.Size.Z));
			if (extent <= 0f)
				throw new InvalidOperationException($"Cloud model '{CloudModelPath}' has no extent.");

			foreach (var meshInstance in meshes)
			{
				var mesh = meshInstance.Mesh;
				var materials = new List<ShaderMaterial>();
				for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++)
				{
					if (meshInstance.GetActiveMaterial(surface) is not StandardMaterial3D source)
						throw new InvalidOperationException($"Cloud model '{CloudModelPath}' has an unsupported material.");
					materials.Add(createMaterial(source));
				}

				if (!FragmentMeshes.TryGetValue(mesh, out var fragments))
				{
					fragments = new CloudFragmentMesh(mesh, bounds.GetCenter(), extent);
					FragmentMeshes.Add(mesh, fragments);
				}
				if (generatedMeshes is null || !generatedMeshes.TryGetValue((mesh, seed), out var generated))
				{
					generated = fragments.Build(regions);
					generatedMeshes?.Add((mesh, seed), generated);
				}
				meshInstance.Mesh = generated;
				for (var surface = 0; surface < materials.Count; surface++)
					meshInstance.SetSurfaceOverrideMaterial(surface, materials[surface]);
				meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
				PresentationLayers.MarkWorld(meshInstance);
			}

			scene.Position = -bounds.GetCenter();
			var cloud = new Node3D
			{
				Scale = Vector3.One * (WorldMapping.CellSize / extent),
			};
			cloud.AddChild(scene);
			return cloud;
		}
		finally
		{
			if (scene.GetParent() is null)
				scene.Free();
		}
	}
}
