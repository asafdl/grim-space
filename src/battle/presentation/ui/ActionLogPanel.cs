using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>
/// Scrollable combat action-log text (placeholder HUD).
/// </summary>
public sealed partial class ActionLogPanel : PanelContainer
{
	private VBoxContainer _column = null!;
	private Label _header = null!;
	private RichTextLabel _log = null!;
	private IReadOnlyList<ActionLog.Entry>? _renderedEntries;
	private int _renderedEntryCount;

	public ActionLogPanel()
	{
		Build();
	}

	public void SetEntries(IReadOnlyList<ActionLog.Entry> entries)
	{
		if (ReferenceEquals(entries, _renderedEntries) && entries.Count == _renderedEntryCount)
			return;

		_renderedEntries = entries;
		_renderedEntryCount = entries.Count;
		_log.Clear();
		if (entries.Count == 0)
		{
			_log.AddText(BattleHudCopy.ActionLogEmpty);
			return;
		}

		for (var i = 0; i < entries.Count; i++)
		{
			if (i > 0)
			{
				_log.Newline();
				_log.Newline();
			}

			Render(entries[i]);
		}
	}

	private void Render(ActionLog.Entry entry)
	{
		if (entry.IsTurnHeader)
		{
			_log.PushColor(_header.GetThemeColor("font_color"));
			_log.PushFontSize(_header.GetThemeFontSize("font_size"));
			_log.AddText(entry.Title);
			_log.Pop();
			_log.Pop();
			return;
		}

		_log.AddText(entry.Title);
		if (entry.Metadata.Count == 0)
			return;

		_log.PushColor(_log.GetThemeColor("metadata_color"));
		_log.PushFontSize(_log.GetThemeConstant("metadata_font_size"));
		foreach (var line in entry.Metadata)
		{
			_log.Newline();
			_log.AddText(line);
		}
		_log.Pop();
		_log.Pop();
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ThemeTypeVariation = HudStyles.DebugHudPanelType;
		HudThemes.Apply(this, HudThemeFamily.Debug);

		_column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_column.AddThemeConstantOverride("separation", 6);
		AddChild(_column);

		_header = new Label
		{
			Text = BattleHudCopy.ActionLogTitle,
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.HudHeadingLabelType,
		};
		_column.AddChild(_header);

		_log = new RichTextLabel
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			ScrollActive = true,
			ScrollFollowing = true,
			SelectionEnabled = false,
			ContextMenuEnabled = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.DebugValueRichTextLabelType,
		};
		_column.AddChild(_log);
	}
}
