using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public enum UiContainerVisual
{
	Modal,
	Framed,
	Simple,
	Technical,
}

public sealed partial class UiContainer : NinePatchRect
{
	private const string ModalTexturePath =
		"res://assets/ui/components/runtime/modal_panel_neutral.png";
	private const string FramedTexturePath =
		"res://assets/ui/components/runtime/surface_framed_neutral.png";
	private const string SimpleTexturePath =
		"res://assets/ui/components/runtime/surface_simple_neutral.png";
	private const string TechnicalTexturePath =
		"res://assets/ui/components/runtime/card_neutral.png";
	private const string ShaderPath =
		"res://assets/ui/components/runtime/palette_remap.gdshader";

	private readonly ShaderMaterial _paletteMaterial;

	public int ContentMarginLeft { get; private set; }
	public int ContentMarginTop { get; private set; }
	public int ContentMarginRight { get; private set; }
	public int ContentMarginBottom { get; private set; }
	public int BodyMarginTop { get; private set; }
	/// <summary>Reserved vertical space for the subtitle line in container artwork.</summary>
	public int HeaderSubtitleBandMinHeight { get; private set; }
	public int HeaderSideSlotWidth { get; private set; }
	public int HeaderTitleSubtitleSeparation { get; private set; }
	public int HeaderTitleOffsetY { get; private set; }
	public int HeaderTitleFontSize { get; private set; }
	public int HeaderSubtitleFontSize { get; private set; }
	public Rect2 DismissButtonOffsets { get; private set; }
	public Color HeaderTitleColor { get; private set; }
	public Color HeaderSubtitleColor { get; private set; }

	public UiContainer() : this(HudPalettes.BlueCyan)
	{
	}

	public UiContainer(HudPalette palette)
	{
		var shader = GD.Load<Shader>(ShaderPath)
			?? throw new InvalidOperationException($"Could not load UI palette shader '{ShaderPath}'.");

		_paletteMaterial = new ShaderMaterial { Shader = shader };
		Material = _paletteMaterial;
		MouseFilter = MouseFilterEnum.Ignore;
		DrawCenter = true;
		SetVisual(UiContainerVisual.Modal);
		SetPalette(palette);
	}

	public void SetVisual(UiContainerVisual visual)
	{
		var texturePath = visual switch
		{
			UiContainerVisual.Framed => FramedTexturePath,
			UiContainerVisual.Simple => SimpleTexturePath,
			UiContainerVisual.Technical => TechnicalTexturePath,
			_ => ModalTexturePath,
		};
		Texture = GD.Load<Texture2D>(texturePath)
			?? throw new InvalidOperationException($"Could not load UI container texture '{texturePath}'.");

		(PatchMarginLeft, PatchMarginTop, PatchMarginRight, PatchMarginBottom) = visual switch
		{
			UiContainerVisual.Framed => (80, 70, 80, 70),
			UiContainerVisual.Simple => (32, 32, 32, 32),
			UiContainerVisual.Technical => (150, 100, 150, 100),
			_ => (170, 130, 170, 110),
		};
		(ContentMarginLeft, ContentMarginTop, ContentMarginRight, ContentMarginBottom) = visual switch
		{
			UiContainerVisual.Framed => (36, 36, 36, 36),
			UiContainerVisual.Simple => (32, 32, 32, 32),
			UiContainerVisual.Technical => (48, 42, 44, 40),
			_ => (70, 48, 70, 48),
		};
		(BodyMarginTop, HeaderTitleSubtitleSeparation, HeaderSubtitleBandMinHeight) = visual switch
		{
			UiContainerVisual.Modal => (14, 42, 28),
			UiContainerVisual.Framed => (10, 4, 24),
			UiContainerVisual.Simple => (10, 4, 24),
			UiContainerVisual.Technical => (10, 4, 24),
			_ => (10, 4, 24),
		};
		HeaderTitleOffsetY = visual == UiContainerVisual.Modal ? 4 : 0;
		HeaderTitleFontSize = visual == UiContainerVisual.Modal ? 32 : 30;
		HeaderSubtitleFontSize = visual == UiContainerVisual.Modal ? 22 : 20;
		HeaderSideSlotWidth = visual switch
		{
			UiContainerVisual.Modal => 120,
			UiContainerVisual.Technical => 72,
			_ => 64,
		};
		DismissButtonOffsets = visual switch
		{
			UiContainerVisual.Modal => new Rect2(-161f, 59f, 120f, 58f),
			UiContainerVisual.Technical => new Rect2(-108f, 32f, 72f, 46f),
			_ => new Rect2(-100f, 24f, 64f, 44f),
		};
	}

	public void SetPalette(HudPalette palette)
	{
		_paletteMaterial.SetShaderParameter(
			"surface_color",
			HudPalettes.WithAlpha(palette.DeepSurface, 1f));
		_paletteMaterial.SetShaderParameter(
			"glow_color",
			HudPalettes.WithAlpha(palette.Highlight, 0.62f));
		_paletteMaterial.SetShaderParameter("edge_color", palette.Primary);
		_paletteMaterial.SetShaderParameter("glow_strength", 0.72f);
		_paletteMaterial.SetShaderParameter("solid_fill", 1f);
		HeaderTitleColor = HudPalettes.Gold.Primary;
		HeaderSubtitleColor = palette.Secondary;
	}
}
