using Godot;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class FacilityOperatorButtonView : TextureButton
{
	private const string HaloShaderPath = "res://assets/shaders/service_button_halo.gdshader";
	private ShaderMaterial _haloMaterial = null!;
	private bool _hovered;
	private bool _focused;

	public override void _Ready()
	{
		var texture = TextureNormal
			?? throw new InvalidOperationException("Facility operator buttons require a normal texture.");
		var clickMask = new Bitmap();
		clickMask.CreateFromImageAlpha(texture.GetImage(), 0.2f);
		TextureClickMask = clickMask;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		FocusMode = FocusModeEnum.All;

		_haloMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(HaloShaderPath),
		};
		Material = _haloMaterial;

		MouseEntered += () =>
		{
			_hovered = true;
			UpdateEmphasis();
		};
		MouseExited += () =>
		{
			_hovered = false;
			UpdateEmphasis();
		};
		FocusEntered += () =>
		{
			_focused = true;
			UpdateEmphasis();
		};
		FocusExited += () =>
		{
			_focused = false;
			UpdateEmphasis();
		};
	}

	private void UpdateEmphasis() =>
		_haloMaterial.SetShaderParameter("emphasis", _hovered || _focused ? 1f : 0f);
}
