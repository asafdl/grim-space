using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.Presentation.Dev;

public sealed partial class UiComponentsPreview : Control
{
	private UiContainer _frame = null!;
	private readonly List<UiButton> _buttons = [];
	private readonly List<UiListItem> _listItems = [];
	private readonly List<UiContainer> _containers = [];
	private readonly List<UiCard> _cards = [];

	public override void _Ready()
	{
		Build();
		ApplyPalette(HudPalettes.BlueCyan);
	}

	private void Build()
	{
		AddChild(new ColorRect
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			Color = new Color(0.005f, 0.012f, 0.025f),
		});

		var scroll = new ScrollContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		AddChild(scroll);

		var page = new MarginContainer
		{
			CustomMinimumSize = new Vector2(1900f, 0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		page.AddThemeConstantOverride("margin_left", 60);
		page.AddThemeConstantOverride("margin_top", 32);
		page.AddThemeConstantOverride("margin_right", 60);
		page.AddThemeConstantOverride("margin_bottom", 48);
		scroll.AddChild(page);

		var gallery = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		gallery.AddThemeConstantOverride("separation", 18);
		page.AddChild(gallery);

		gallery.AddChild(CreateHeading("UI COMPONENTS", 30));
		var description = CreateHeading(
			"Reusable container, button, list-item, and card components",
			15);
		description.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(Colors.White, 0.56f));
		gallery.AddChild(description);
		gallery.AddChild(CreatePaletteToolbar());
		gallery.AddChild(CreateHeading("BUTTONS IN MODAL CONTAINER", 18));

		var frameCenter = new CenterContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		frameCenter.AddChild(CreateFramePreview());
		gallery.AddChild(frameCenter);

		gallery.AddChild(CreateHeading("CONTAINER OPTIONS", 20));
		var containers = CreateComparisonRow();
		containers.AddChild(CreateContainerPreview("MODAL", UiContainerVisual.Modal));
		containers.AddChild(CreateContainerPreview("FRAMED", UiContainerVisual.Framed));
		containers.AddChild(CreateContainerPreview("TECHNICAL", UiContainerVisual.Technical));
		gallery.AddChild(containers);

		gallery.AddChild(CreateHeading("LANDSCAPE SIDEBAR", 20));
		var sidebar = CreateContainerPreview(
			"LANDSCAPE",
			UiContainerVisual.SidebarLandscape);
		sidebar.CustomMinimumSize = new Vector2(900f, 300f);
		gallery.AddChild(sidebar);

		gallery.AddChild(CreateHeading("CARD OPTIONS", 20));
		var cards = CreateComparisonRow();
		cards.AddChild(CreateCardPreview("ANGLED", UiCardVisual.Angled));
		cards.AddChild(CreateCardPreview("FRAMED", UiCardVisual.Framed));
		cards.AddChild(CreateCardPreview("TECHNICAL", UiCardVisual.Technical));
		gallery.AddChild(cards);

		gallery.AddChild(CreateHeading("LIST ITEM COMPARISON", 20));
		var listDescription = CreateHeading(
			"Same content and states, using three silhouettes from the UI kit",
			14);
		listDescription.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(Colors.White, 0.56f));
		gallery.AddChild(listDescription);

		var styles = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		styles.AddThemeConstantOverride("separation", 28);
		styles.AddChild(CreateStyleColumn("ANGLED TAG", UiListItemVisual.Tag));
		styles.AddChild(CreateStyleColumn("OPEN BRACKET", UiListItemVisual.Bracket));
		styles.AddChild(CreateStyleColumn("TECHNICAL PANEL", UiListItemVisual.Technical));
		gallery.AddChild(styles);
	}

	private Control CreatePaletteToolbar()
	{
		var toolbar = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			CustomMinimumSize = new Vector2(0f, 50f),
		};
		toolbar.AddThemeConstantOverride("separation", 14);
		toolbar.AddChild(CreateHeading("PALETTE", 15));
		toolbar.AddChild(CreateButton(
			"Blue Cyan",
			() => ApplyPalette(HudPalettes.BlueCyan)));
		toolbar.AddChild(CreateButton(
			"Gold",
			() => ApplyPalette(HudPalettes.Gold)));
		toolbar.AddChild(CreateButton(
			"Purple Red",
			() => ApplyPalette(HudPalettes.PurpleRed)));
		return toolbar;
	}

	private Control CreateFramePreview()
	{
		var stage = new Control { CustomMinimumSize = new Vector2(780f, 600f) };

		_frame = new UiContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		_containers.Add(_frame);
		stage.AddChild(_frame);

		var content = new MarginContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		content.AddThemeConstantOverride("margin_left", 70);
		content.AddThemeConstantOverride("margin_top", 64);
		content.AddThemeConstantOverride("margin_right", 70);
		content.AddThemeConstantOverride("margin_bottom", 48);
		stage.AddChild(content);

		var layout = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		layout.AddThemeConstantOverride("separation", 12);
		content.AddChild(layout);

		layout.AddChild(CreateHeading("CONTAINER", 25));
		var subtitle = CreateHeading("Frame and reusable control variants", 14);
		subtitle.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(Colors.White, 0.58f));
		layout.AddChild(subtitle);

		layout.AddChild(CreateHeading("PRIMARY BUTTONS", 15));
		var primaryStyles = CreateCenteredRow();
		primaryStyles.AddChild(CreateStyledButton("Action", UiButtonVisual.Action));
		primaryStyles.AddChild(CreateStyledButton("Compact", UiButtonVisual.Compact));
		primaryStyles.AddChild(CreateStyledButton("Back", UiButtonVisual.Back));
		layout.AddChild(primaryStyles);

		layout.AddChild(CreateHeading("TECHNICAL BUTTONS", 15));
		var technicalStyles = CreateCenteredRow();
		technicalStyles.AddChild(CreateStyledButton("Technical", UiButtonVisual.Technical));
		technicalStyles.AddChild(CreateStyledButton("Framed", UiButtonVisual.Framed));
		technicalStyles.AddChild(CreateStyledButton("Wide", UiButtonVisual.WideBeveled));
		layout.AddChild(technicalStyles);

		layout.AddChild(CreateHeading("ACTION STATES", 15));
		var states = CreateCenteredRow();
		states.AddChild(CreateActionButton("Cancel", HudActionKind.Secondary));
		states.AddChild(CreateActionButton("Delete", HudActionKind.Destructive));
		states.AddChild(CreateActionButton("Confirm", HudActionKind.Primary));
		var disabled = CreateActionButton("Disabled", HudActionKind.Primary);
		disabled.Disabled = true;
		states.AddChild(disabled);
		layout.AddChild(states);

		var close = CreateActionButton("X", HudActionKind.Secondary);
		close.SetIntegrated(true);
		close.SetFontSize(23);
		close.CustomMinimumSize = new Vector2(120f, 58f);
		close.Position = new Vector2(619f, 59f);
		stage.AddChild(close);

		return stage;
	}

	private Control CreateContainerPreview(string heading, UiContainerVisual visual)
	{
		var stage = new Control { CustomMinimumSize = new Vector2(540f, 340f) };
		var container = new UiContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		container.SetVisual(visual);
		_containers.Add(container);
		stage.AddChild(container);

		var labels = new VBoxContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		labels.AddChild(CreateHeading(heading, 21));
		var subtitle = CreateHeading("CONTAINER SURFACE", 13);
		subtitle.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(Colors.White, 0.58f));
		labels.AddChild(subtitle);
		stage.AddChild(labels);
		return stage;
	}

	private UiCard CreateCardPreview(string heading, UiCardVisual visual)
	{
		var card = new UiCard(
			"SHIP SYSTEM UPGRADE",
			heading,
			"Improves performance while preserving the current component layout.",
			"800 CR")
		{
			CustomMinimumSize = new Vector2(540f, 340f),
		};
		card.SetVisual(visual);
		_cards.Add(card);
		return card;
	}

	private Control CreateStyleColumn(string heading, UiListItemVisual visual)
	{
		var column = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(560f, 0f),
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		column.AddThemeConstantOverride("separation", 8);
		column.AddChild(CreateHeading(heading, 16));

		column.AddChild(CreateListItem(
			visual,
			"HULL REPAIR",
			"Restore 25 hull integrity",
			"320 CR"));

		var selected = CreateListItem(
			visual,
			"REACTOR TUNING",
			"+1 maximum action point",
			"800 CR");
		selected.SetSelected(true);
		column.AddChild(selected);

		var unaffordable = CreateListItem(
			visual,
			"PLASMA ARMOR",
			"+15% thermal resistance",
			"2,100 CR");
		unaffordable.SetAffordable(false);
		column.AddChild(unaffordable);

		var disabled = CreateListItem(
			visual,
			"QUANTUM DRIVE",
			"Requires navigation core III",
			"LOCKED");
		disabled.SetEnabled(false);
		column.AddChild(disabled);

		return column;
	}

	private UiListItem CreateListItem(
		UiListItemVisual visual,
		string title,
		string metadata,
		string trailingValue)
	{
		var item = new UiListItem(title, metadata, trailingValue);
		item.SetVisual(visual);
		_listItems.Add(item);
		return item;
	}

	private UiButton CreateButton(string text, Action onPressed)
	{
		var button = CreateActionButton(text, HudActionKind.Secondary);
		button.SetVisual(UiButtonVisual.Technical);
		button.Pressed += onPressed;
		return button;
	}

	private UiButton CreateStyledButton(string text, UiButtonVisual visual)
	{
		var button = CreateActionButton(text, HudActionKind.Secondary);
		button.SetVisual(visual);
		return button;
	}

	private UiButton CreateActionButton(string text, HudActionKind kind)
	{
		var button = new UiButton(text, kind);
		_buttons.Add(button);
		return button;
	}

	private void ApplyPalette(HudPalette palette)
	{
		foreach (var button in _buttons)
			button.SetPalette(palette);
		foreach (var item in _listItems)
			item.SetPalette(palette);
		foreach (var container in _containers)
			container.SetPalette(palette);
		foreach (var card in _cards)
			card.SetPalette(palette);
	}

	private static Label CreateHeading(string text, int fontSize)
	{
		var label = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(Colors.White, 0.76f));
		return label;
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

	private static HBoxContainer CreateComparisonRow()
	{
		var row = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 28);
		return row;
	}
}
