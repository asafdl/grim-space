using Godot;

namespace GrimSpace.Presentation.Menu;

/// <summary>Stretches the dust particle band between its anchor markers whenever the menu art resizes.</summary>
public partial class MenuDustBand : Control
{
	[Export(PropertyHint.Range, "0.005,0.05,0.005")]
	private float _endHalfThicknessRatio = 0.015f;

	private Control _start = null!;
	private Control _end = null!;
	private Control _wide = null!;
	private GpuParticles2D _particles = null!;
	private ParticleProcessMaterial _material = null!;

	public override void _Ready()
	{
		_start = GetNode<Control>("DustBandStart");
		_end = GetNode<Control>("DustBandEnd");
		_wide = GetNode<Control>("DustBandWide");
		_particles = GetNode<GpuParticles2D>("DustParticles");
		_material = (ParticleProcessMaterial)_particles.ProcessMaterial.Duplicate();
		_particles.ProcessMaterial = _material;
		Resized += UpdateLayout;
		CallDeferred(MethodName.UpdateLayout);
	}

	private void UpdateLayout()
	{
		if (Size.X < 1f || Size.Y < 1f)
			return;

		var start = _start.Position;
		var delta = _end.Position - start;
		var length = delta.Length();
		if (length < 1f)
			return;

		var tangent = delta / length;
		var normal = new Vector2(-tangent.Y, tangent.X);
		var startHalf = Mathf.Abs((_wide.Position - start).Dot(normal));
		var bandHalfThickness = (startHalf + Size.Y * _endHalfThicknessRatio) * 0.5f;
		var visibilityPad = length * 0.5f + bandHalfThickness + 64f;

		_particles.Position = start + delta * 0.5f;
		_particles.Rotation = delta.Angle();
		_material.EmissionBoxExtents = new Vector3(length * 0.5f, bandHalfThickness, 1f);
		_particles.VisibilityRect = new Rect2(-visibilityPad, -visibilityPad, visibilityPad * 2f, visibilityPad * 2f);
	}
}
