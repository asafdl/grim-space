using Godot;

namespace GrimSpace.Components;

public enum SciFiButtonVisual
{
	Action,
	Compact,
	Back,
	Technical,
	Framed,
	WideBeveled,
}

public sealed partial class SciFiButton : BaseButton
{
	private const string ActionTexturePath =
		"res://assets/ui/sci_fi_components/runtime/button_base.png";
	private const string BackTexturePath =
		"res://assets/ui/sci_fi_components/runtime/back_tag_base.png";
	private const string CompactTexturePath =
		"res://assets/ui/sci_fi_components/runtime/compact_button_base.png";
	private const string TechnicalTexturePath =
		"res://assets/ui/sci_fi_components/runtime/button_technical_base.png";
	private const string FramedTexturePath =
		"res://assets/ui/sci_fi_components/runtime/button_framed_base.png";
	private const string WideBeveledTexturePath =
		"res://assets/ui/sci_fi_components/runtime/button_wide_beveled_base.png";
	private const string ShaderPath =
		"res://assets/ui/sci_fi_components/runtime/button_palette.gdshader";

	private readonly TextureRect _background;
	private readonly Label _label;
	private readonly ShaderMaterial _paletteMaterial;
	private HudPalette _palette;
	private HudActionKind _kind;
	private SciFiButtonVisual _visual = SciFiButtonVisual.Action;
	private bool _integrated;

	public SciFiButton() : this("", HudActionKind.Secondary, HudPalettes.BlueCyan)
	{
	}

	public SciFiButton(
		string text,
		HudActionKind kind = HudActionKind.Secondary,
		HudPalette? palette = null)
	{
		var texture = LoadTexture(ActionTexturePath);
		var shader = GD.Load<Shader>(ShaderPath)
			?? throw new InvalidOperationException($"Could not load sci-fi palette shader '{ShaderPath}'.");

		_palette = palette ?? HudPalettes.BlueCyan;
		_kind = kind;
		CustomMinimumSize = new Vector2(140f, 44f);
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;

		_paletteMaterial = new ShaderMaterial { Shader = shader };
		_background = new TextureRect
		{
			Texture = texture,
			Material = _paletteMaterial,
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		};
		AddChild(_background);

		_label = new Label
		{
			Text = text,
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 16f,
			OffsetRight = -16f,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(_label);

		MouseEntered += RefreshVisualState;
		MouseExited += RefreshVisualState;
		FocusEntered += RefreshVisualState;
		FocusExited += RefreshVisualState;
		ButtonDown += RefreshVisualState;
		ButtonUp += RefreshVisualState;
		RefreshVisualState();
	}

	public string Text
	{
		get => _label.Text;
		set => _label.Text = value;
	}

	public void SetPalette(HudPalette palette)
	{
		_palette = palette;
		RefreshVisualState();
	}

	public void SetActionKind(HudActionKind kind)
	{
		_kind = kind;
		RefreshVisualState();
	}

	public void SetFontSize(int fontSize) =>
		_label.AddThemeFontSizeOverride("font_size", fontSize);

	public void SetVisual(SciFiButtonVisual visual)
	{
		_visual = visual;
		_background.Texture = LoadTexture(visual switch
		{
			SciFiButtonVisual.Back => BackTexturePath,
			SciFiButtonVisual.Compact => CompactTexturePath,
			SciFiButtonVisual.Technical => TechnicalTexturePath,
			SciFiButtonVisual.Framed => FramedTexturePath,
			SciFiButtonVisual.WideBeveled => WideBeveledTexturePath,
			_ => ActionTexturePath,
		});
		CustomMinimumSize = visual switch
		{
			SciFiButtonVisual.Back => new Vector2(142f, 46f),
			SciFiButtonVisual.Compact => new Vector2(128f, 46f),
			SciFiButtonVisual.Technical => new Vector2(152f, 46f),
			SciFiButtonVisual.Framed => new Vector2(146f, 48f),
			SciFiButtonVisual.WideBeveled => new Vector2(154f, 54f),
			_ => new Vector2(140f, 44f),
		};
		RefreshVisualState();
	}

	public void SetIntegrated(bool integrated)
	{
		_integrated = integrated;
		_background.Visible = !integrated;
		RefreshVisualState();
	}

	private void RefreshVisualState()
	{
		var edge = _kind == HudActionKind.Destructive
			? new Color(0.92f, 0.32f, 0.34f)
			: _kind == HudActionKind.Primary
				? _palette.Highlight
				: _palette.Secondary;
		var surface = _palette.DeepSurface;
		var brightness = 1f;

		if (Disabled)
		{
			brightness = 0.5f;
		}
		else if (ButtonPressed)
		{
			surface = surface.Lerp(edge, 0.24f);
			brightness = 1.16f;
		}
		else if (IsHovered() || HasFocus())
		{
			surface = surface.Lerp(edge, 0.16f);
			brightness = 1.1f;
		}
		else if (_integrated)
		{
			brightness = 0.82f;
		}

		_paletteMaterial.SetShaderParameter("surface_color", surface);
		_paletteMaterial.SetShaderParameter("edge_color", edge);
		_paletteMaterial.SetShaderParameter(
			"palette_mix",
			_visual == SciFiButtonVisual.Back
				|| _kind == HudActionKind.Destructive
				|| _palette != HudPalettes.BlueCyan
				? 1f
				: 0f);
		_paletteMaterial.SetShaderParameter("brightness", brightness);
		_label.Modulate = Disabled
			? HudPalettes.WithAlpha(Colors.White, 0.38f)
			: _integrated && !IsHovered() && !HasFocus()
				? edge.Lerp(Colors.White, 0.35f)
				: Colors.White;
	}

	private static Texture2D LoadTexture(string path) =>
		GD.Load<Texture2D>(path)
		?? throw new InvalidOperationException($"Could not load sci-fi button texture '{path}'.");
}
