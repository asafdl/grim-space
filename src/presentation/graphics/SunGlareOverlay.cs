using Godot;

namespace GrimSpace.Presentation.Graphics;

/// <summary>
/// Screen space glare and exposure dip for a star.
///
/// The star's own shaders draw what the star is; this draws what the star does
/// to the camera - the halo that survives anywhere on screen, the chromatic
/// bleed, the burn-in core, and the "squint" that darkens the frame when the
/// star is filling it.
///
/// It is a CanvasLayer so it is unaffected by the 3D camera, and it follows
/// the star's projected position every frame.
/// </summary>
public sealed partial class SunGlareOverlay : CanvasLayer
{
	private const string GlareShaderPath = "res://assets/shaders/sun_glare.gdshader";

	/// <summary>The node the glare is centred on (the star).</summary>
	public Node3D? Target { get; set; }

	/// <summary>Radius of the star, in the same units as the target's scale.</summary>
	public float TargetRadius { get; set; } = 1f;

	public float Intensity { get; set; } = 1f;

	/// <summary>How much the frame squints.  Driven by the star's apparent size.</summary>
	public float DipStrength { get; set; }

	public float VignetteStrength { get; set; } = 0.35f;

	public float StreakAmount { get; set; } = 0.25f;

	public float Chromatic { get; set; } = 0.35f;

	public float Breathe { get; set; } = 0.10f;

	private ShaderMaterial? _glareMaterial;
	private ShaderMaterial? _dipMaterial;

	public override void _Ready()
	{
		Layer = 10;
		AddChild(CreateQuad(_glareMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(GlareShaderPath),
		}, blendAdd: true));
		AddChild(CreateQuad(_dipMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(GlareShaderPath),
		}, blendAdd: false));
	}

	public override void _Process(double delta)
	{
		if (_glareMaterial is null || _dipMaterial is null)
			return;

		var camera = GetViewport().GetCamera3D();
		if (camera is null || Target is null || !IsInstanceValid(Target))
			return;

		var viewport = GetViewport().GetVisibleRect().Size;
		if (viewport.X <= 0f || viewport.Y <= 0f)
			return;

		var centre = Target.GlobalPosition;
		var projected = camera.UnprojectPosition(centre);
		var edge = camera.UnprojectPosition(
			centre + camera.GlobalTransform.Basis.X * TargetRadius);
		var radiusPixels = (edge - projected).Length();

		var screen = new Vector2(projected.X / viewport.X, projected.Y / viewport.Y);
		var radius = radiusPixels / viewport.X;

		// The squint: the more of the frame the star eats, the harder the eye
		// has to work.
		var apparent = radius * viewport.X;
		var squint = Mathf.Clamp((apparent - 0.05f) * 1.1f, 0f, 0.26f);

		foreach (var (material, mode) in new[] { (_glareMaterial, 0), (_dipMaterial, 1) })
		{
			material.SetShaderParameter("sun_screen", screen);
			material.SetShaderParameter("sun_radius", Mathf.Max(radius, 0.0005f));
			material.SetShaderParameter("aspect", viewport.X / Mathf.Max(viewport.Y, 1f));
			material.SetShaderParameter("intensity", Intensity);
			material.SetShaderParameter("breathe", Breathe);
			material.SetShaderParameter("streak_amount", StreakAmount);
			material.SetShaderParameter("chromatic", Chromatic);
			material.SetShaderParameter("dip_strength", Mathf.Max(squint, DipStrength));
			material.SetShaderParameter("vignette_strength", VignetteStrength);
			material.SetShaderParameter("mode", mode);
		}
	}

	private static ColorRect CreateQuad(ShaderMaterial material, bool blendAdd)
	{
		material.SetShaderParameter("mode", blendAdd ? 0 : 1);
		var rect = new ColorRect
		{
			Color = new Color(1f, 1f, 1f, 1f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material = material,
		};
		// A Control in a CanvasLayer has to be told to fill the viewport, or it
		// stays a zero sized rect at the origin and draws nothing.
		rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		rect.AnchorRight = 1f;
		rect.AnchorBottom = 1f;
		rect.GrowHorizontal = Control.GrowDirection.Both;
		rect.GrowVertical = Control.GrowDirection.Both;
		return rect;
	}
}
