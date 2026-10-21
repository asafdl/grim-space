using Godot;

namespace GrimSpace.Components;

public sealed partial class SciFiFrame : NinePatchRect
{
	private const string TexturePath =
		"res://assets/ui/sci_fi_components/runtime/modal_panel_neutral.png";
	private const string ShaderPath =
		"res://assets/ui/sci_fi_components/runtime/palette_remap.gdshader";

	private readonly ShaderMaterial _paletteMaterial;

	public SciFiFrame() : this(HudPalettes.BlueCyan)
	{
	}

	public SciFiFrame(HudPalette palette)
	{
		Texture = GD.Load<Texture2D>(TexturePath)
			?? throw new InvalidOperationException($"Could not load sci-fi frame texture '{TexturePath}'.");
		var shader = GD.Load<Shader>(ShaderPath)
			?? throw new InvalidOperationException($"Could not load sci-fi palette shader '{ShaderPath}'.");

		_paletteMaterial = new ShaderMaterial { Shader = shader };
		Material = _paletteMaterial;
		MouseFilter = MouseFilterEnum.Ignore;
		DrawCenter = true;
		PatchMarginLeft = 170;
		PatchMarginTop = 130;
		PatchMarginRight = 170;
		PatchMarginBottom = 110;
		SetPalette(palette);
	}

	public void SetPalette(HudPalette palette)
	{
		_paletteMaterial.SetShaderParameter(
			"surface_color",
			HudPalettes.WithAlpha(palette.DeepSurface, 0.94f));
		_paletteMaterial.SetShaderParameter(
			"glow_color",
			HudPalettes.WithAlpha(palette.Highlight, 0.62f));
		_paletteMaterial.SetShaderParameter("edge_color", palette.Primary);
		_paletteMaterial.SetShaderParameter("glow_strength", 0.72f);
	}
}
