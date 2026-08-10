using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class WeaponPreviewMaterials
{
	public static readonly Color CementedTint = new(1f, 0.7f, 0.82f, 0.38f);

	private const float AimFill = 0.45f;
	private const float CementedFill = 0.02f;

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
				// Mesh body between dots. Aim ~0.45; cemented ~0 keeps only dots.
				uniform float fill = 0.45;

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
					float density = mix(fill, 1.0, dots);

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
		ApplyAim(material, tint, strength: 1f);
		return material;
	}

	public static void ApplyAim(ShaderMaterial material, Color tint, float strength)
	{
		material.SetShaderParameter("tint", tint);
		material.SetShaderParameter("fill", AimFill);
		material.SetShaderParameter("strength", strength);
	}

	public static void ApplyCemented(ShaderMaterial material, float strength = 0.9f)
	{
		material.SetShaderParameter("tint", CementedTint);
		material.SetShaderParameter("fill", CementedFill);
		material.SetShaderParameter("strength", strength);
	}
}
