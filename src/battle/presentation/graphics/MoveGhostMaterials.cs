using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class MoveGhostMaterials
{
	private static readonly Color PassiveNeutral = new(0.48f, 0.58f, 0.68f);
	private static readonly Color SelectedNeutral = new(0.32f, 0.82f, 0.94f);
	private static readonly Color PassiveDorsal = new(0.88f, 0.6f, 0.06f);
	private static readonly Color SelectedDorsal = new(1f, 0.82f, 0.16f);

	private static Shader? _dorsalShader;

	public static ShaderMaterial Create(Transform3D meshToShip, Aabb hullBounds)
	{
		_dorsalShader ??= new Shader
		{
			Code =
				"""
				shader_type spatial;

				render_mode
					unshaded,
					cull_back,
					shadows_disabled,
					fog_disabled;

				uniform mat4 mesh_to_ship;
				uniform float dorsal_start;
				uniform vec4 neutral_color : source_color;
				uniform vec4 dorsal_color : source_color;
				uniform float fill = 0.5;

				varying vec3 ship_pos;

				void vertex()
				{
					ship_pos = (mesh_to_ship * vec4(VERTEX, 1.0)).xyz;
				}

				void fragment()
				{
					float dorsal = step(dorsal_start, ship_pos.y);
					vec4 tint = mix(neutral_color, dorsal_color, dorsal);

					vec3 cell = fract(ship_pos / 0.28) - 0.5;
					float dots = 1.0 - smoothstep(0.11, 0.23, length(cell));
					float brightness = mix(fill, 1.0, dots);

					ALBEDO = tint.rgb * brightness;
				}
				""",
		};

		var material = new ShaderMaterial { Shader = _dorsalShader };
		material.SetShaderParameter("mesh_to_ship", meshToShip);
		material.SetShaderParameter(
			"dorsal_start",
			hullBounds.Position.Y + hullBounds.Size.Y * 0.42f);
		Apply(material, selected: false);
		return material;
	}

	public static void Apply(ShaderMaterial material, bool selected)
	{
		material.SetShaderParameter(
			"neutral_color",
			selected ? SelectedNeutral : PassiveNeutral);
		material.SetShaderParameter(
			"dorsal_color",
			selected ? SelectedDorsal : PassiveDorsal);
		material.SetShaderParameter("fill", selected ? 0.62f : 0.48f);
	}
}
