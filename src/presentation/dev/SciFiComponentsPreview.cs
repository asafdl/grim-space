using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Dev;

public sealed partial class SciFiComponentsPreview : Control
{
	private SciFiFrame _frame = null!;
	private readonly List<SciFiButton> _buttons = [];

	public override void _Ready()
	{
		Build();
		ApplyPalette(HudPalettes.BlueCyan);
	}

	private void Build()
	{
		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			Color = new Color(0.005f, 0.012f, 0.025f),
		};
		AddChild(backdrop);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		AddChild(center);

		var stage = new Control { CustomMinimumSize = new Vector2(780f, 600f) };
		center.AddChild(stage);

		_frame = new SciFiFrame
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		stage.AddChild(_frame);

		var content = new MarginContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		content.AddThemeConstantOverride("margin_left", 70);
		content.AddThemeConstantOverride("margin_top", 62);
		content.AddThemeConstantOverride("margin_right", 70);
		content.AddThemeConstantOverride("margin_bottom", 52);
		stage.AddChild(content);

		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 22);
		content.AddChild(layout);

		var title = new Label
		{
			Text = "COMPONENT SHOWCASE",
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(0f, 42f),
		};
		title.AddThemeFontSizeOverride("font_size", 26);
		layout.AddChild(title);

		var description = new Label
		{
			Text = "Palette-driven frame and action controls",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		description.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(Colors.White, 0.62f));
		layout.AddChild(description);

		var paletteLabel = new Label
		{
			Text = "PALETTE",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		layout.AddChild(paletteLabel);

		var paletteRow = CreateCenteredRow();
		paletteRow.AddChild(CreateButton(
			"Blue Cyan",
			HudActionKind.Secondary,
			() => ApplyPalette(HudPalettes.BlueCyan)));
		paletteRow.AddChild(CreateButton(
			"Gold",
			HudActionKind.Secondary,
			() => ApplyPalette(HudPalettes.Gold)));
		paletteRow.AddChild(CreateButton(
			"Purple Red",
			HudActionKind.Secondary,
			() => ApplyPalette(HudPalettes.PurpleRed)));
		layout.AddChild(paletteRow);

		var styleLabel = new Label
		{
			Text = "BUTTON STYLES",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		layout.AddChild(styleLabel);

		var styleRow = CreateCenteredRow();
		styleRow.AddChild(CreateStyledButton("Technical", SciFiButtonVisual.Technical));
		styleRow.AddChild(CreateStyledButton("Framed", SciFiButtonVisual.Framed));
		styleRow.AddChild(CreateStyledButton("Wide", SciFiButtonVisual.WideBeveled));
		layout.AddChild(styleRow);

		var spacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
		layout.AddChild(spacer);

		var stateLabel = new Label
		{
			Text = "ACTION STATES",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		layout.AddChild(stateLabel);

		var actionRow = CreateCenteredRow();
		actionRow.AddChild(CreateButton("Cancel", HudActionKind.Secondary));
		actionRow.AddChild(CreateButton("Delete", HudActionKind.Destructive));
		actionRow.AddChild(CreateButton("Confirm", HudActionKind.Primary));
		var disabled = CreateButton("Disabled", HudActionKind.Primary);
		disabled.Disabled = true;
		actionRow.AddChild(disabled);
		layout.AddChild(actionRow);

		var close = CreateButton("X", HudActionKind.Secondary);
		close.SetIntegrated(true);
		close.SetFontSize(23);
		close.CustomMinimumSize = new Vector2(120f, 58f);
		close.Position = new Vector2(619f, 59f);
		stage.AddChild(close);

		var back = CreateButton("BACK", HudActionKind.Secondary);
		back.SetVisual(SciFiButtonVisual.Back);
		back.Position = new Vector2(60f, 65f);
		stage.AddChild(back);
	}

	private SciFiButton CreateButton(
		string text,
		HudActionKind kind,
		Action? onPressed = null)
	{
		var button = new SciFiButton(text, kind);
		if (onPressed is not null)
			button.Pressed += onPressed;
		_buttons.Add(button);
		return button;
	}

	private SciFiButton CreateStyledButton(string text, SciFiButtonVisual visual)
	{
		var button = CreateButton(text, HudActionKind.Secondary);
		button.SetVisual(visual);
		return button;
	}

	private void ApplyPalette(HudPalette palette)
	{
		_frame.SetPalette(palette);
		foreach (var button in _buttons)
			button.SetPalette(palette);
	}

	private static HBoxContainer CreateCenteredRow()
	{
		var row = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		row.AddThemeConstantOverride("separation", 14);
		return row;
	}
}
