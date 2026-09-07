using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public static class HudWidgets
{
	public static VBoxContainer CreateCardList()
	{
		var list = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		list.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		return list;
	}

	public static Control CreateCard(
		string title,
		IReadOnlyList<HudTextLine> rows,
		Action onPressed)
	{
		var panel = new PanelContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			FocusMode = Control.FocusModeEnum.All,
		};
		HudStyles.SetPanelVariation(panel, "Card");

		var margin = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		margin.AddThemeConstantOverride("margin_left", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		panel.AddChild(margin);

		var column = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		column.AddThemeConstantOverride("separation", 6);
		margin.AddChild(column);

		var titleLabel = new Label
		{
			Text = title,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = "CardTitle",
		};
		column.AddChild(titleLabel);

		foreach (var row in rows)
			column.AddChild(CreateTextLine(row));

		panel.MouseEntered += () => HudStyles.SetPanelVariation(panel, "CardHover");
		panel.MouseExited += () => HudStyles.SetPanelVariation(panel, "Card");
		panel.GuiInput += @event =>
		{
			if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
				return;

			HudStyles.SetPanelVariation(panel, "CardPressed");
			onPressed();
		};
		panel.FocusEntered += () => HudStyles.SetPanelVariation(panel, "CardHover");

		return panel;
	}

	public static Control CreateSection(
		string heading,
		string body,
		bool scrollBody = false,
		HudTextRole bodyRole = HudTextRole.Body)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, "Section");

		var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		panel.AddChild(margin);

		var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		column.AddThemeConstantOverride("separation", 8);
		margin.AddChild(column);

		var headingLabel = new Label
		{
			Text = heading.ToUpperInvariant(),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = "SectionHeading",
		};
		column.AddChild(headingLabel);

		if (scrollBody)
		{
			var scroll = new ScrollContainer
			{
				CustomMinimumSize = new Vector2(0, HudStyles.BodyScrollMinHeight),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};
			column.AddChild(scroll);
			scroll.AddChild(CreateBodyLabel(body, bodyRole));
		}
		else
			column.AddChild(CreateBodyLabel(body, bodyRole));

		return panel;
	}

	public static Control CreateStatusPanel(HudStatusKind kind, string message)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, HudStyles.StatusPanelVariation(kind));

		var label = new Label
		{
			Text = message,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = "Status",
		};
		ApplyStatusLabelStyle(label, kind);

		var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		margin.AddChild(label);
		panel.AddChild(margin);
		return panel;
	}

	public static Control CreateWarningPanel(string message)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, "Warning");

		var label = new Label
		{
			Text = message,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = "Body",
		};

		var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.Margin);
		margin.AddChild(label);
		panel.AddChild(margin);
		return panel;
	}

	private static Label CreateTextLine(HudTextLine row)
	{
		var label = new Label
		{
			Text = row.Text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HudStyles.ApplyTextRole(label, row.Role);

		if (row.ColorOverride is { } color)
			label.AddThemeColorOverride("font_color", color);

		return label;
	}

	private static Label CreateBodyLabel(string body, HudTextRole bodyRole = HudTextRole.Body)
	{
		var label = new Label
		{
			Text = body,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HudStyles.ApplyTextRole(label, bodyRole);
		return label;
	}

	private static void ApplyStatusLabelStyle(Label label, HudStatusKind kind)
	{
		var textRole = kind switch
		{
			HudStatusKind.Success => HudTextRole.Success,
			HudStatusKind.Warning => HudTextRole.Warning,
			HudStatusKind.Error => HudTextRole.Danger,
			_ => HudTextRole.Metadata,
		};
		HudStyles.ApplyTextRole(label, textRole);
	}

	public static MapHudPanelView CreateInformativePanel(
		string sectionTitle,
		string? panelVariation = null,
		string? headingVariation = null,
		int outerPadding = 14)
	{
		var panel = new PanelContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			ThemeTypeVariation = panelVariation ?? HudStyles.InformativePanelType,
		};

		var column = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		column.AddThemeConstantOverride("separation", 0);
		panel.AddChild(column);

		var headerPadding = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		headerPadding.AddThemeConstantOverride("margin_left", outerPadding);
		headerPadding.AddThemeConstantOverride("margin_right", outerPadding);
		headerPadding.AddThemeConstantOverride("margin_top", outerPadding);
		headerPadding.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		headerPadding.AddChild(CreateInformativeSectionHeader(
			sectionTitle,
			headingVariation ?? HudStyles.HudHeadingLabelType));
		column.AddChild(headerPadding);

		var bodyPadding = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		bodyPadding.AddThemeConstantOverride("margin_left", outerPadding);
		bodyPadding.AddThemeConstantOverride("margin_right", outerPadding);
		bodyPadding.AddThemeConstantOverride("margin_top", 0);
		bodyPadding.AddThemeConstantOverride("margin_bottom", outerPadding);
		column.AddChild(bodyPadding);

		var body = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		body.AddThemeConstantOverride("separation", 0);
		bodyPadding.AddChild(body);

		return new MapHudPanelView(panel, body);
	}

	public static MapHudPanelView CreateInformativeListPanel(
		string sectionTitle,
		string outerPanelVariation,
		bool includeCounter = false)
	{
		var panel = new PanelContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			ThemeTypeVariation = outerPanelVariation,
		};

		var column = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		column.AddThemeConstantOverride("separation", 0);
		panel.AddChild(column);

		var headerPadding = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		headerPadding.AddThemeConstantOverride("margin_left", HudStyles.InformativeListPadding);
		headerPadding.AddThemeConstantOverride("margin_right", HudStyles.InformativeListPadding);
		headerPadding.AddThemeConstantOverride("margin_top", HudStyles.InformativeListPadding);
		headerPadding.AddThemeConstantOverride("margin_bottom", HudStyles.InformativeListHeaderBottomPadding);

		var headerRow = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		headerRow.AddThemeConstantOverride("separation", 8);
		headerPadding.AddChild(headerRow);

		headerRow.AddChild(new Label
		{
			Text = sectionTitle.ToUpperInvariant(),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.InformativeSectionTitleLabelType,
		});

		Label? counterLabel = null;
		if (includeCounter)
		{
			headerRow.AddChild(new Label
			{
				Text = "//",
				MouseFilter = Control.MouseFilterEnum.Ignore,
				ThemeTypeVariation = HudStyles.InformativeItemSeparatorLabelType,
			});

			counterLabel = new Label
			{
				Text = "00",
				MouseFilter = Control.MouseFilterEnum.Ignore,
				ThemeTypeVariation = HudStyles.InformativeCounterLabelType,
			};
			headerRow.AddChild(counterLabel);
		}

		column.AddChild(headerPadding);

		var dividerPadding = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		dividerPadding.AddThemeConstantOverride("margin_left", HudStyles.InformativeListPadding);
		dividerPadding.AddThemeConstantOverride("margin_right", HudStyles.InformativeListPadding);
		dividerPadding.AddChild(CreateInformativeHairline());
		column.AddChild(dividerPadding);

		var bodyPadding = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		bodyPadding.AddThemeConstantOverride("margin_left", HudStyles.InformativeListPadding);
		bodyPadding.AddThemeConstantOverride("margin_right", HudStyles.InformativeListPadding);
		bodyPadding.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		bodyPadding.AddThemeConstantOverride("margin_bottom", HudStyles.InformativeListPadding);
		column.AddChild(bodyPadding);

		var body = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeListVBoxType,
		};
		body.AddThemeConstantOverride("separation", 0);
		bodyPadding.AddChild(body);

		return new MapHudPanelView(panel, body, counterLabel);
	}

	private static Control CreateInformativeSectionHeader(
		string sectionTitle,
		string headingThemeType)
	{
		var row = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 8);

		row.AddChild(new ColorRect
		{
			CustomMinimumSize = new Vector2(HudStyles.SectionAccentWidth, 20),
			Color = HudStyles.AccentCyan,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		});

		var heading = new Label
		{
			Text = sectionTitle.ToUpperInvariant(),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = headingThemeType,
		};
		row.AddChild(heading);

		return row;
	}

	public static Control CreateInformativeHairline() =>
		new ColorRect
		{
			CustomMinimumSize = new Vector2(0, 1),
			Color = HudStyles.InformativeHairline,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

	public static MapHudPanelView CreateHudPanel(string heading, HudThemeFamily family)
	{
		var panel = new PanelContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
		};
		HudStyles.SetPanelVariation(panel, HudStyles.PanelVariation(family));

		var column = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		column.AddThemeConstantOverride("separation", family == HudThemeFamily.Debug ? 6 : HudStyles.HalfMargin);
		panel.AddChild(column);

		var headingLabel = new Label
		{
			Text = heading.ToUpperInvariant(),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.HudHeadingVariation(family),
		};
		column.AddChild(headingLabel);

		var body = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		body.AddThemeConstantOverride("separation", family == HudThemeFamily.Debug ? 6 : HudStyles.HalfMargin);
		column.AddChild(body);

		return new MapHudPanelView(panel, body);
	}

	public static Button CreateCompactButton(string text, Action onPressed)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(0, 44),
		};
		HudStyles.StyleButton(button, HudActionKind.Secondary);
		button.Pressed += onPressed;
		return button;
	}

	public static HudBannerView CreateTopBanner(string title, string subtitle)
	{
		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(520, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		HudStyles.SetPanelVariation(panel, "Banner");

		var content = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		content.AddThemeConstantOverride("separation", 4);
		panel.AddChild(content);

		var titleLabel = new Label
		{
			Text = title,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ThemeTypeVariation = "BannerTitle",
		};
		content.AddChild(titleLabel);

		var subtitleLabel = new Label
		{
			Text = subtitle,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		HudStyles.ApplyTextRole(subtitleLabel, HudTextRole.Body);
		content.AddChild(subtitleLabel);

		return new HudBannerView(panel, titleLabel, subtitleLabel);
	}

	public static Button CreateMenuButton(string text, string tooltip, Action onPressed)
	{
		var button = new Button
		{
			Text = text,
			TooltipText = tooltip,
			CustomMinimumSize = new Vector2(220, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		HudStyles.StyleButton(button, HudActionKind.Secondary);
		button.Pressed += onPressed;
		return button;
	}
}
