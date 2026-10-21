using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public enum UiCardVisual
{
	Angled,
	Framed,
	Technical,
}

public sealed partial class UiCard : BaseButton
{
	private const string AngledTexturePath =
		"res://assets/ui/components/runtime/surface_angled_neutral.png";
	private const string FramedTexturePath =
		"res://assets/ui/components/runtime/surface_framed_neutral.png";
	private const string TechnicalTexturePath =
		"res://assets/ui/components/runtime/card_neutral.png";
	private const string ShaderPath =
		"res://assets/ui/components/runtime/palette_remap.gdshader";

	private readonly TextureRect _background;
	private readonly ShaderMaterial _paletteMaterial;
	private readonly Control _content;
	private readonly Label _titleLabel;
	private readonly Label _subtitleLabel;
	private readonly Label _descriptionLabel;
	private readonly Label _trailingLabel;
	private readonly MarginContainer _margin;
	private HudPalette _palette;
	private bool _selected;
	private bool _affordable = true;
	private Color? _trailingColorOverride;

	public UiCard() : this("", "", "", "", null, HudPalettes.BlueCyan)
	{
	}

	public UiCard(
		string title,
		string subtitle,
		string description,
		string trailingValue,
		Texture2D? portrait = null,
		HudPalette? palette = null)
	{
		var shader = GD.Load<Shader>(ShaderPath)
			?? throw new InvalidOperationException($"Could not load UI palette shader '{ShaderPath}'.");

		_palette = palette ?? HudPalettes.BlueCyan;
		CustomMinimumSize = new Vector2(580f, 379f);
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;

		_paletteMaterial = new ShaderMaterial { Shader = shader };
		_background = new TextureRect
		{
			Texture = LoadTexture(AngledTexturePath),
			Material = _paletteMaterial,
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
		};
		AddChild(_background);

		_margin = new MarginContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(_margin);

		_content = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_content.AddThemeConstantOverride("separation", 24);
		_margin.AddChild(_content);

		if (portrait is not null)
		{
			_content.AddChild(new TextureRect
			{
				Texture = portrait,
				CustomMinimumSize = new Vector2(138f, 172f),
				MouseFilter = MouseFilterEnum.Ignore,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			});
		}

		var text = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		text.AddThemeConstantOverride("separation", 8);
		_content.AddChild(text);

		_titleLabel = new Label
		{
			Text = title,
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Left,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_titleLabel.AddThemeFontSizeOverride("font_size", 15);
		text.AddChild(_titleLabel);

		_subtitleLabel = new Label
		{
			Text = subtitle,
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Left,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_subtitleLabel.AddThemeFontSizeOverride("font_size", 15);
		text.AddChild(_subtitleLabel);

		_descriptionLabel = new Label
		{
			Text = description,
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Left,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_descriptionLabel.AddThemeFontSizeOverride("font_size", 19);
		text.AddChild(_descriptionLabel);

		_trailingLabel = new Label
		{
			Text = trailingValue,
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_trailingLabel.AddThemeFontSizeOverride("font_size", 19);
		text.AddChild(_trailingLabel);

		SetVisual(UiCardVisual.Angled);

		MouseEntered += RefreshVisualState;
		MouseExited += RefreshVisualState;
		FocusEntered += RefreshVisualState;
		FocusExited += RefreshVisualState;
		ButtonDown += RefreshVisualState;
		ButtonUp += RefreshVisualState;
		RefreshVisualState();
	}

	public void SetPalette(HudPalette palette)
	{
		_palette = palette;
		RefreshVisualState();
	}

	public void SetVisual(UiCardVisual visual)
	{
		_background.Texture = LoadTexture(visual switch
		{
			UiCardVisual.Framed => FramedTexturePath,
			UiCardVisual.Technical => TechnicalTexturePath,
			_ => AngledTexturePath,
		});
		ApplyContentMargins(visual);
	}

	public void SetTrailingAlignment(HorizontalAlignment alignment) =>
		_trailingLabel.HorizontalAlignment = alignment;

	public void SetTrailingColor(Color color)
	{
		_trailingColorOverride = color;
		RefreshVisualState();
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		RefreshVisualState();
	}

	public void SetAffordable(bool affordable)
	{
		_affordable = affordable;
		RefreshVisualState();
	}

	public void SetEnabled(bool enabled)
	{
		Disabled = !enabled;
		RefreshVisualState();
	}

	public void SetInteractive(bool interactive)
	{
		FocusMode = interactive ? FocusModeEnum.All : FocusModeEnum.None;
		MouseFilter = interactive ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
		MouseDefaultCursorShape = interactive ? CursorShape.PointingHand : CursorShape.Arrow;
	}

	private void RefreshVisualState()
	{
		var edge = _affordable
			? _selected
				? _palette.Highlight
				: _palette.Primary
			: new Color(0.92f, 0.32f, 0.34f);
		var glowStrength = 0.5f;

		if (Disabled)
			glowStrength = 0.12f;
		else if (ButtonPressed)
			glowStrength = 1f;
		else if (_selected || IsHovered() || HasFocus())
			glowStrength = _selected ? 0.9f : 0.72f;

		_paletteMaterial.SetShaderParameter(
			"surface_color",
			HudPalettes.WithAlpha(_palette.DeepSurface, Disabled ? 0.55f : 0.94f));
		_paletteMaterial.SetShaderParameter(
			"glow_color",
			HudPalettes.WithAlpha(edge, Disabled ? 0.18f : 0.62f));
		_paletteMaterial.SetShaderParameter("edge_color", edge);
		_paletteMaterial.SetShaderParameter("glow_strength", glowStrength);
		_titleLabel.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(_palette.Secondary, 0.82f));
		_subtitleLabel.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(_palette.Secondary, 0.82f));
		_descriptionLabel.AddThemeColorOverride("font_color", _palette.Primary);
		_trailingLabel.AddThemeColorOverride(
			"font_color",
			_trailingColorOverride ?? _palette.Highlight);
		_content.Modulate = Disabled
			? HudPalettes.WithAlpha(Colors.White, 0.36f)
			: _affordable
				? Colors.White
				: new Color(1f, 0.58f, 0.58f);
	}

	private void ApplyContentMargins(UiCardVisual visual)
	{
		var (left, top, right, bottom) = visual switch
		{
			UiCardVisual.Framed => (44, 48, 44, 44),
			UiCardVisual.Technical => (60, 52, 56, 48),
			_ => (52, 54, 48, 50),
		};
		_margin.AddThemeConstantOverride("margin_left", left);
		_margin.AddThemeConstantOverride("margin_top", top);
		_margin.AddThemeConstantOverride("margin_right", right);
		_margin.AddThemeConstantOverride("margin_bottom", bottom);
	}

	private static Texture2D LoadTexture(string path) =>
		GD.Load<Texture2D>(path)
		?? throw new InvalidOperationException($"Could not load UI card texture '{path}'.");
}
