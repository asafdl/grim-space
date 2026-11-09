using Godot;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class ShieldFaceMaterials
{
	private static Shader? _shader;
	public static Shader Shader => _shader ??= GD.Load<Shader>("res://assets/shaders/shield_energy.gdshader")
		?? throw new InvalidOperationException("Could not load the shield energy shader.");

	public static Color TeamTint(ETeam? team) => team switch
	{
		ETeam.Player => new Color(0.52f, 0.68f, 0.76f),
		ETeam.Enemy => new Color(0.76f, 0.59f, 0.49f),
		_ => new Color(0.64f, 0.67f, 0.7f),
	};

	public static Color TintFor(Color teamTint, int points, int maxPoints, int temporaryPoints)
	{
		var fraction = maxPoints > 0 ? Mathf.Clamp((float)points / maxPoints, 0f, 1f) : 0f;
		var tint = teamTint * Mathf.Lerp(0.55f, 1f, fraction);
		if (temporaryPoints > 0)
			tint = teamTint.Lightened(0.15f);
		return new Color(tint, 1f);
	}

	public static ShaderMaterial Create(Aabb bounds, Color tint)
	{
		var material = new ShaderMaterial { Shader = Shader };
		material.SetShaderParameter("team_color", tint);
		material.SetShaderParameter("bubble_center", bounds.GetCenter());
		material.SetShaderParameter("bubble_radius", ShieldBubbleMesh.Radius(bounds));
		return material;
	}
}
