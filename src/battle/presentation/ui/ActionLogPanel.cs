using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>
/// Scrollable combat action-log text (placeholder HUD).
/// </summary>
public sealed partial class ActionLogPanel : PanelContainer
{
	private ScrollContainer _scroll = null!;
	private Label _label = null!;
	private int _lastLineCount;
	private int _chaseTailGeneration;

	public ActionLogPanel()
	{
		Build();
	}

	public void SetLines(IReadOnlyList<string> lines)
	{
		var text = lines.Count == 0 ? BattleHudCopy.ActionLogEmpty : string.Join('\n', lines);
		_label.Text = text;

		if (lines.Count <= _lastLineCount)
			return;

		_lastLineCount = lines.Count;
		_chaseTailGeneration++;
		var generation = _chaseTailGeneration;
		Callable.From(() => ChaseTail(generation)).CallDeferred();
	}

	private void ChaseTail(int generation)
	{
		if (generation != _chaseTailGeneration)
			return;

		_scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		margin.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(margin);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 6);
		column.MouseFilter = MouseFilterEnum.Ignore;
		margin.AddChild(column);

		var header = new Label
		{
			Text = BattleHudCopy.ActionLogTitle,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		header.AddThemeFontSizeOverride("font_size", 14);
		column.AddChild(header);

		_scroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddChild(_scroll);

		_label = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_label.AddThemeFontSizeOverride("font_size", 13);
		_label.Resized += () => ChaseTail(_chaseTailGeneration);
		_scroll.AddChild(_label);
	}
}
