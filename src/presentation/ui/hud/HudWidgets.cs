using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public static class HudWidgets
{
	public static VBoxContainer CreateCardList(Viewport? viewport = null)
	{
		var list = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		list.AddThemeConstantOverride("separation", HudStyles.Margin(viewport) / 2);
		return list;
	}

	public static Control CreateCard(
		string title,
		IReadOnlyList<HudTextLine> rows,
		Action onPressed,
		Viewport? viewport = null)
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
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin(viewport) / 2);
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin(viewport) / 2);
		margin.AddThemeConstantOverride("margin_top", HudStyles.Margin(viewport) / 2);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.Margin(viewport) / 2);
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
		};
		HudStyles.ApplyFont(titleLabel, HudFontRole.CardTitle, viewport);
		column.AddChild(titleLabel);

		foreach (var row in rows)
			column.AddChild(CreateTextLine(row, viewport));

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
		Viewport? viewport = null,
		HudTextRole bodyRole = HudTextRole.Body)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, "Section");

		var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin(viewport) / 2);
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin(viewport) / 2);
		margin.AddThemeConstantOverride("margin_top", HudStyles.Margin(viewport) / 2);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.Margin(viewport) / 2);
		panel.AddChild(margin);

		var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		column.AddThemeConstantOverride("separation", 8);
		margin.AddChild(column);

		var headingLabel = new Label
		{
			Text = heading.ToUpperInvariant(),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HudStyles.ApplyFont(headingLabel, HudFontRole.SectionHeading, viewport);
		column.AddChild(headingLabel);

		if (scrollBody)
		{
			var scroll = new ScrollContainer
			{
				CustomMinimumSize = new Vector2(0, HudStyles.FontSize(HudFontRole.Body, viewport) * 6),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};
			column.AddChild(scroll);
			scroll.AddChild(CreateBodyLabel(body, viewport, bodyRole));
		}
		else
			column.AddChild(CreateBodyLabel(body, viewport, bodyRole));

		return panel;
	}

	public static Control CreateStatusPanel(HudStatusKind kind, string message, Viewport? viewport = null)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, HudStyles.StatusPanelVariation(kind));

		var label = new Label
		{
			Text = message,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		ApplyStatusLabelStyle(label, kind, viewport);

		var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin(viewport));
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin(viewport));
		margin.AddThemeConstantOverride("margin_top", HudStyles.Margin(viewport) / 2);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.Margin(viewport) / 2);
		margin.AddChild(label);
		panel.AddChild(margin);
		return panel;
	}

	public static Control CreateWarningPanel(string message, Viewport? viewport = null)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, "Warning");

		var label = new Label
		{
			Text = message,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HudStyles.ApplyFont(label, HudFontRole.Body, viewport);

		var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin(viewport));
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin(viewport));
		margin.AddThemeConstantOverride("margin_top", HudStyles.Margin(viewport));
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.Margin(viewport));
		margin.AddChild(label);
		panel.AddChild(margin);
		return panel;
	}

	private static Label CreateTextLine(HudTextLine row, Viewport? viewport)
	{
		var label = new Label
		{
			Text = row.Text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		if (row.Role == HudTextRole.Emphasis)
			HudStyles.ApplyFont(label, HudFontRole.Emphasis, viewport);
		else
			HudStyles.ApplyTextRole(label, row.Role, viewport);

		if (row.ColorOverride is { } color)
			label.AddThemeColorOverride("font_color", color);

		return label;
	}

	private static Label CreateBodyLabel(
		string body,
		Viewport? viewport,
		HudTextRole bodyRole = HudTextRole.Body)
	{
		var label = new Label
		{
			Text = body,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HudStyles.ApplyTextRole(label, bodyRole, viewport);
		return label;
	}

	private static void ApplyStatusLabelStyle(Label label, HudStatusKind kind, Viewport? viewport)
	{
		var textRole = kind switch
		{
			HudStatusKind.Success => HudTextRole.Success,
			HudStatusKind.Warning => HudTextRole.Warning,
			HudStatusKind.Error => HudTextRole.Danger,
			_ => HudTextRole.Metadata,
		};
		HudStyles.ApplyTextRole(label, textRole, viewport);
		label.AddThemeFontSizeOverride("font_size", HudStyles.FontSize(HudFontRole.Status, viewport));
	}
}
