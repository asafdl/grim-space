using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>
/// Scrollable combat action-log text (placeholder HUD).
/// </summary>
public sealed partial class ActionLogPanel : PanelContainer
{
	private const int HeaderFontDesign = 18;
	private const int BodyFontDesign = 16;
	private const int MarginDesign = 10;
	private const int SeparationDesign = 6;

	private MarginContainer _margin = null!;
	private VBoxContainer _column = null!;
	private Label _header = null!;
	private ScrollContainer _scroll = null!;
	private Label _label = null!;
	private int _lastLineCount;
	private int _chaseTailGeneration;

	public ActionLogPanel()
	{
		Build();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
			ApplyScale();
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

		_margin = new MarginContainer();
		_margin.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(_margin);

		_column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_margin.AddChild(_column);

		_header = new Label
		{
			Text = BattleHudCopy.ActionLogTitle,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_column.AddChild(_header);

		_scroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_column.AddChild(_scroll);

		_label = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_label.Resized += () => ChaseTail(_chaseTailGeneration);
		_scroll.AddChild(_label);

		Callable.From(ApplyScale).CallDeferred();
	}

	private void ApplyScale()
	{
		var viewport = GetViewport();
		_margin.AddThemeConstantOverride("margin_left", UiScale.Margin(MarginDesign, viewport));
		_margin.AddThemeConstantOverride("margin_right", UiScale.Margin(MarginDesign, viewport));
		_margin.AddThemeConstantOverride("margin_top", UiScale.Margin(8, viewport));
		_margin.AddThemeConstantOverride("margin_bottom", UiScale.Margin(8, viewport));
		_column.AddThemeConstantOverride("separation", UiScale.Margin(SeparationDesign, viewport));
		_header.AddThemeFontSizeOverride("font_size", UiScale.Font(HeaderFontDesign, viewport));
		_label.AddThemeFontSizeOverride("font_size", UiScale.Font(BodyFontDesign, viewport));
	}
}
