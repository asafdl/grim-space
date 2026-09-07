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
