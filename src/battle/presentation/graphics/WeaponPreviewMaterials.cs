using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>Shared soft dotted volume look for weapon aim previews.</summary>
internal static class WeaponPreviewMaterials
{
	private static Shader? _dottedShader;

	public static ShaderMaterial CreateDotted(Color tint)
	{
		_dottedShader ??= new Shader
		{
			Code =
				"""
				shader_type spatial;

				render_mode
					unshaded,
					blend_mix,
					depth_draw_never,
					cull_disabled,
					shadows_disabled,
					fog_disabled;

				uniform vec4 tint : source_color =
					vec4(0.55, 0.82, 1.0, 0.42);

				uniform float strength = 1.0;

				varying vec3 local_pos;

				void vertex()
				{
					local_pos = VERTEX;
				}

				void fragment()
				{
					float facing = abs(
						dot(normalize(NORMAL), normalize(VIEW)));

					float soft_edge =
						smoothstep(0.02, 0.65, facing);

					vec3 grid = local_pos / 0.32;
					vec3 cell = fract(grid) - 0.5;
					float dots = 1.0 - smoothstep(0.10, 0.22, length(cell));
					float density = mix(0.45, 1.0, dots);

					ALBEDO = tint.rgb;
					EMISSION = tint.rgb * (0.35 + 0.55 * dots);
					ALPHA =
						tint.a
						* COLOR.a
						* soft_edge
						* density
						* strength;
				}
				""",
		};

		var material = new ShaderMaterial { Shader = _dottedShader };
		material.SetShaderParameter("tint", tint);
		return material;
	}
}
