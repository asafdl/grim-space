using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class FacilityOperatorButtonView : TextureButton
{
	private const string HaloShaderPath = "res://assets/shaders/service_button_halo.gdshader";
	private const string ContractIndicatorPath = "res://assets/ui/map/contract-offer-icon.svg";
	private const int IndicatorPx = 48;
	private ShaderMaterial _haloMaterial = null!;
	private Texture2D _contractIndicatorTexture = null!;
	private TextureRect _roleIndicator = null!;
	private Func<EFacilityOperatorRole>? _roleResolver;
	private Func<EFacilityOperatorRole, string>? _titleResolver;
	private EFacilityOperatorRole? _displayedRole;
	private bool _hovered;
	private bool _focused;

	public override void _Ready()
	{
		var texture = TextureNormal
			?? throw new InvalidOperationException("Facility operator buttons require a normal texture.");
		var image = (Image)texture.GetImage().Duplicate();
		if (FlipH)
			image.FlipX();
		if (FlipV)
			image.FlipY();
		var clickMask = new Bitmap();
		clickMask.CreateFromImageAlpha(image, 0.08f);
		TextureClickMask = clickMask;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		FocusMode = FocusModeEnum.All;

		_haloMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(HaloShaderPath),
		};
		Material = _haloMaterial;
		_contractIndicatorTexture = SvgIconLoader.LoadRaw(ContractIndicatorPath, IndicatorPx);
		_roleIndicator = new TextureRect
		{
			Texture = _contractIndicatorTexture,
			CustomMinimumSize = new Vector2(IndicatorPx, IndicatorPx),
			Size = new Vector2(IndicatorPx, IndicatorPx),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
			ZIndex = 10,
			Visible = false,
		};
		AddChild(_roleIndicator);

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

	public override void _Process(double delta)
	{
		base._Process(delta);
		_roleIndicator.Position = new Vector2((Size.X - IndicatorPx) * 0.5f, -IndicatorPx - 8f);
		if (_roleResolver is null)
			return;

		var role = _roleResolver();
		if (_displayedRole == role)
			return;

		_displayedRole = role;
		_roleIndicator.Visible = role is EFacilityOperatorRole.Contracts
			or EFacilityOperatorRole.StoryContact;
		if (_titleResolver is not null)
			TooltipText = _titleResolver(role);
	}

	public void ConfigureRolePresentation(
		Func<EFacilityOperatorRole> roleResolver,
		Func<EFacilityOperatorRole, string> titleResolver)
	{
		ArgumentNullException.ThrowIfNull(roleResolver);
		ArgumentNullException.ThrowIfNull(titleResolver);
		_roleResolver = roleResolver;
		_titleResolver = titleResolver;
		_displayedRole = null;
	}

	private void UpdateEmphasis() =>
		_haloMaterial.SetShaderParameter("emphasis", _hovered || _focused ? 1f : 0f);
}
