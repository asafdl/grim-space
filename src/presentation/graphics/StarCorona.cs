using System;
using Godot;

namespace GrimSpace.Presentation.Graphics;

/// <summary>Reusable visual-only corona. Add beside an opaque sun sphere.</summary>
public static class StarCorona
{
	private const string ShaderPath = "res://assets/shaders/star_corona.gdshader";

	public static MeshInstance3D Create(
		float sunRadius,
		Color color,
		float brightness = 0.65f,
		float width = 0.3f,
		float speed = 0.08f,
		float seed = 0f,
		float irregularity = 0.55f)
	{
		if (!float.IsFinite(sunRadius) || sunRadius <= 0f)
			throw new ArgumentOutOfRangeException(nameof(sunRadius));

		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(ShaderPath),
		};
		material.SetShaderParameter("corona_color", color);
		material.SetShaderParameter("brightness", Mathf.Max(0f, brightness));
		material.SetShaderParameter("width", Mathf.Clamp(width, 0.05f, 0.65f));
		material.SetShaderParameter("speed", Mathf.Max(0f, speed));
		material.SetShaderParameter("seed", seed);
		material.SetShaderParameter("irregularity", Mathf.Clamp(irregularity, 0f, 1f));

		return new MeshInstance3D
		{
			Name = "Corona",
			Mesh = new QuadMesh { Size = Vector2.One * sunRadius * 4f },
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			CustomAabb = new Aabb(
				Vector3.One * -sunRadius * 2f,
				Vector3.One * sunRadius * 4f),
		};
	}
}
