using Godot;
using GrimSpace.Battle.NonUnits;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>Noise-eroded ice-blue clouds for static cryo-plumes; built once at battle setup.</summary>
public partial class CryoPlumeView : Node3D
{
	private const string CloudModelPath = "res://assets/models/spaceobjects/cloud.glb";
	private static readonly Color IceColor = new(0.35f, 0.78f, 1f, 0.55f);
	private static readonly Shader CloudShader = new()
	{
		Code =
			"""
			shader_type spatial;
			render_mode unshaded, cull_disabled, shadows_disabled, depth_draw_never;

			uniform sampler2D albedo_texture : source_color;
			uniform vec4 ice_color : source_color;
			uniform vec3 plume_center;
			uniform float cell_size;
			uniform vec3 noise_offset;
			varying vec3 plume_position;

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
				ALBEDO = ice_color.rgb * mix(0.7, 1.0, fine);
			}
			""",
	};

	public void Build(IEnumerable<CryoPlume> plumes)
	{
		var cells = plumes.Select(plume => plume.Center).ToArray();
		if (cells.Length == 0)
			return;

		var model = GD.Load<PackedScene>(CloudModelPath)
			?? throw new InvalidOperationException($"Could not load cloud model '{CloudModelPath}'.");
		foreach (var cell in cells)
		{
			var rng = new RandomNumberGenerator
			{
				Seed = (ulong)(uint)cell.X * 73856093
					^ (ulong)(uint)cell.Y * 19349663
					^ (ulong)(uint)cell.Z * 83492791,
			};
			var noiseOffset = new Vector3(
				rng.RandfRange(0f, 64f), rng.RandfRange(0f, 64f), rng.RandfRange(0f, 64f));
			var scene = model.Instantiate<Node3D>();
			try
			{
				scene.RotateY(rng.RandfRange(0f, Mathf.Tau));
				scene.RotateX(rng.RandfRange(-0.3f, 0.3f));
				scene.RotateZ(rng.RandfRange(-0.3f, 0.3f));
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
					for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++)
					{
						if (meshes[i].GetActiveMaterial(surface) is not StandardMaterial3D source)
							throw new InvalidOperationException($"Cloud model '{CloudModelPath}' has an unsupported material.");

						var material = new ShaderMaterial { Shader = CloudShader };
						material.SetShaderParameter("albedo_texture", source.AlbedoTexture);
						material.SetShaderParameter("ice_color", IceColor);
						material.SetShaderParameter("plume_center", WorldMapping.ToWorld(cell));
						material.SetShaderParameter("cell_size", WorldMapping.CellSize);
						material.SetShaderParameter("noise_offset", noiseOffset);
						meshes[i].SetSurfaceOverrideMaterial(surface, material);
					}

					meshes[i].CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
					PresentationLayers.MarkWorld(meshes[i]);
				}

				var extent = Mathf.Max(bounds.Size.X, Mathf.Max(bounds.Size.Y, bounds.Size.Z));
				if (extent <= 0f)
					throw new InvalidOperationException($"Cloud model '{CloudModelPath}' has no extent.");

				var scale = WorldMapping.CellSize / extent;
				var cloud = new Node3D
				{
					Name = "CryoPlumeCloud",
					Position = WorldMapping.ToWorld(cell) - bounds.GetCenter() * scale,
					Scale = Vector3.One * scale,
				};
				cloud.AddChild(scene);
				AddChild(cloud);
			}
			finally
			{
				if (scene.GetParent() is null)
					scene.Free();
			}
		}
	}
}
