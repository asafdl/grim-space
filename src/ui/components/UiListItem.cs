using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public enum UiListItemVisual
{
	Tag,
	Bracket,
	Technical,
}

public partial class UiListItem : BaseButton
{
	private const string TagTexturePath =
		"res://assets/ui/components/runtime/list_item_base.png";
	private const string BracketTexturePath =
		"res://assets/ui/components/runtime/list_item_bracket_base.png";
	private const string TechnicalTexturePath =
		"res://assets/ui/components/runtime/list_item_technical_base.png";
	private const string ShaderPath =
		"res://assets/ui/components/runtime/button_palette.gdshader";

	private readonly TextureRect _background;
	private readonly ShaderMaterial _paletteMaterial;
	private readonly MarginContainer _margin;
	private readonly Control _content;
	private readonly VBoxContainer _textColumn;
	private readonly Label _titleLabel;
	private readonly Label _metadataLabel;
	private readonly VBoxContainer _detailHost;
	private readonly RichTextLabel _detailRichText;
	private readonly VBoxContainer _trailingHost;
	private readonly Label _trailingLabel;
	private Control? _customDetail;
	private Control? _customTrailing;
	private HudPalette _palette;
	private bool _selected;
	private bool _affordable = true;
	private Color? _trailingColorOverride;
	private Color? _detailColorOverride;

	protected ShaderMaterial PaletteMaterial => _paletteMaterial;

	public UiListItem() : this("", "", "", HudPalettes.BlueCyan)
	{
	}

	public UiListItem(
		string title,
		string metadata,
		string trailingValue,
		HudPalette? palette = null)
	{
		var shader = GD.Load<Shader>(ShaderPath)
			?? throw new InvalidOperationException($"Could not load sci-fi palette shader '{ShaderPath}'.");

		_palette = palette ?? HudPalettes.BlueCyan;
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;

		_paletteMaterial = new ShaderMaterial { Shader = shader };
		_background = new TextureRect
		{
			Texture = LoadTexture(TagTexturePath),
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
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		_content.AddThemeConstantOverride("separation", 20);
		_margin.AddChild(_content);

		_textColumn = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		_textColumn.AddThemeConstantOverride("separation", 6);
		_content.AddChild(_textColumn);

		_titleLabel = new Label
		{
			Text = title,
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_titleLabel.AddThemeFontSizeOverride("font_size", 20);
		_titleLabel.AddThemeConstantOverride("line_spacing", 2);
		_textColumn.AddChild(_titleLabel);

		_metadataLabel = new Label
		{
			Text = metadata,
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_metadataLabel.AddThemeFontSizeOverride("font_size", 17);
		_metadataLabel.AddThemeConstantOverride("line_spacing", 2);
		_metadataLabel.Visible = !string.IsNullOrEmpty(metadata);
		_textColumn.AddChild(_metadataLabel);

		_detailHost = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = false,
		};
		_textColumn.AddChild(_detailHost);

		_detailRichText = new RichTextLabel
		{
			BbcodeEnabled = false,
			FitContent = true,
			ScrollActive = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = false,
			ThemeTypeVariation = HudStyles.InformativeItemDescriptionRichTextLabelType,
		};
		_detailRichText.AddThemeFontSizeOverride("normal_font_size", 16);
		_detailHost.AddChild(_detailRichText);

		_trailingHost = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(104f, 0f),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			Visible = !string.IsNullOrEmpty(trailingValue),
		};
		_content.AddChild(_trailingHost);

		_trailingLabel = new Label
		{
			Text = trailingValue,
			MouseFilter = MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = !string.IsNullOrEmpty(trailingValue),
		};
		_trailingLabel.AddThemeFontSizeOverride("font_size", 20);
		_trailingHost.AddChild(_trailingLabel);

		SetVisual(UiListItemVisual.Tag);

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

	public void SetVisual(UiListItemVisual visual)
	{
		_background.Texture = LoadTexture(visual switch
		{
			UiListItemVisual.Bracket => BracketTexturePath,
			UiListItemVisual.Technical => TechnicalTexturePath,
			_ => TagTexturePath,
		});
		ApplyContentMargins();
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

	public void SetTrailingColor(Color? color)
	{
		_trailingColorOverride = color;
		RefreshVisualState();
	}

	public void SetDetail(string detail)
	{
		ClearCustomDetail();
		_detailRichText.BbcodeEnabled = false;
		_detailRichText.Text = detail;
		var visible = !string.IsNullOrEmpty(detail);
		_detailRichText.Visible = visible;
		_detailHost.Visible = visible;
		UpdateMinimumSize();
	}

	public void SetDetailBbcode(string bbcode)
	{
		ClearCustomDetail();
		_detailRichText.BbcodeEnabled = true;
		_detailRichText.Text = bbcode;
		var visible = !string.IsNullOrEmpty(bbcode);
		_detailRichText.Visible = visible;
		_detailHost.Visible = visible;
		UpdateMinimumSize();
	}

	public void SetDetailColor(Color? color)
	{
		_detailColorOverride = color;
		RefreshVisualState();
	}

	public void SetDetailContent(Control content)
	{
		ArgumentNullException.ThrowIfNull(content);
		ClearCustomDetail();
		_detailRichText.Visible = false;
		_customDetail = content;
		content.MouseFilter = MouseFilterEnum.Ignore;
		_detailHost.AddChild(content);
		_detailHost.Visible = true;
		UpdateMinimumSize();
	}

	public void SetTrailingContent(Control content)
	{
		ArgumentNullException.ThrowIfNull(content);
		ClearCustomTrailing();
		_trailingLabel.Visible = false;
		_customTrailing = content;
		content.MouseFilter = MouseFilterEnum.Ignore;
		_trailingHost.AddChild(content);
		_trailingHost.Visible = true;
		UpdateMinimumSize();
	}

	public void SetEnabled(bool enabled)
	{
		Disabled = !enabled;
		RefreshVisualState();
	}

	private void RefreshVisualState()
	{
		var edge = _affordable
			? _selected
				? _palette.Highlight
				: _palette.Secondary
			: new Color(0.92f, 0.32f, 0.34f);
		var surface = _palette.DeepSurface;
		var brightness = 1f;

		if (Disabled)
			brightness = 0.42f;
		else if (ButtonPressed)
		{
			surface = surface.Lerp(edge, 0.28f);
			brightness = 1.14f;
		}
		else if (_selected || IsHovered() || HasFocus())
		{
			surface = surface.Lerp(edge, _selected ? 0.24f : 0.14f);
			brightness = _selected ? 1.12f : 1.07f;
		}

		_paletteMaterial.SetShaderParameter("surface_color", surface);
		_paletteMaterial.SetShaderParameter("edge_color", edge);
		_paletteMaterial.SetShaderParameter("palette_mix", 1f);
		_paletteMaterial.SetShaderParameter("brightness", brightness);
		_titleLabel.AddThemeColorOverride("font_color", _palette.Primary);
		_metadataLabel.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(_palette.Secondary, 0.82f));
		_detailRichText.AddThemeColorOverride(
			"default_color",
			_detailColorOverride ?? HudPalettes.WithAlpha(_palette.Secondary, 0.72f));
		_trailingLabel.AddThemeColorOverride(
			"font_color",
			_trailingColorOverride ?? _palette.Highlight);
		_content.Modulate = Disabled
			? HudPalettes.WithAlpha(Colors.White, 0.36f)
			: _affordable
				? Colors.White
				: new Color(1f, 0.58f, 0.58f);
	}

	private void ClearCustomDetail()
	{
		if (_customDetail is null)
			return;

		_customDetail.QueueFree();
		_customDetail = null;
	}

	private void ClearCustomTrailing()
	{
		if (_customTrailing is null)
			return;

		_customTrailing.QueueFree();
		_customTrailing = null;
	}

	private void ApplyContentMargins()
	{
		SetContentMargins(60, 48, 12, 12);
	}

	protected void SetContentMargins(int left, int right, int top, int bottom)
	{
		_margin.AddThemeConstantOverride("margin_left", left);
		_margin.AddThemeConstantOverride("margin_right", right);
		_margin.AddThemeConstantOverride("margin_top", top);
		_margin.AddThemeConstantOverride("margin_bottom", bottom);
		UpdateMinimumSize();
	}

	public override Vector2 _GetMinimumSize() => _margin.GetCombinedMinimumSize();

	protected void HideBackground() => _background.Hide();

	protected void AddBehindContent(Control control)
	{
		AddChild(control);
		MoveChild(control, _margin.GetIndex());
	}

	private static Texture2D LoadTexture(string path) =>
		GD.Load<Texture2D>(path)
		?? throw new InvalidOperationException($"Could not load sci-fi list item texture '{path}'.");
}
