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
}
