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
	private const string DipShaderPath = "res://assets/shaders/sun_dip.gdshader";

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
			Shader = GD.Load<Shader>(DipShaderPath),
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
		// has to work.  `radius` is the star's apparent radius as a fraction
		// of the frame width — the threshold law is written in that fraction,
		// so a distant star squints mildly and a disc-filling star squints
		// hard, no matter the resolution.
		var visibleRadius = radius * 0.5f; // radiusPixels is a diameter; the law speaks of the disc's radius
		var squint = Mathf.Clamp((visibleRadius - 0.05f) * 1.1f, 0f, 0.26f);

		var aspect = viewport.X / Mathf.Max(viewport.Y, 1f);
		if (_glareMaterial is not null)
		{
			_glareMaterial.SetShaderParameter("sun_screen", screen);
			_glareMaterial.SetShaderParameter("sun_radius", Mathf.Max(radius, 0.0005f));
			_glareMaterial.SetShaderParameter("aspect", aspect);
			_glareMaterial.SetShaderParameter("intensity", Intensity);
			_glareMaterial.SetShaderParameter("breathe", Breathe);
			_glareMaterial.SetShaderParameter("streak_amount", StreakAmount);
			_glareMaterial.SetShaderParameter("chromatic", Chromatic);
		}
		if (_dipMaterial is not null)
		{
			_dipMaterial.SetShaderParameter("aspect", aspect);
			_dipMaterial.SetShaderParameter("dip_strength", Mathf.Max(squint, DipStrength));
			_dipMaterial.SetShaderParameter("vignette_strength", VignetteStrength);
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
