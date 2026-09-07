using Godot;
using GrimSpace.Presentation.Ui.Hud;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class ObjectivesHud : MarginContainer
{
	private const int PanelWidth = 500;

	private Theme _theme = null!;
	private VBoxContainer _entriesHost = null!;
	private Label _emptyLabel = null!;
	private Label _headerCountLabel = null!;
	private string _lastSignature = "";

	public override void _Ready()
	{
		_theme = HudThemes.Apply(this, HudThemeFamily.Informative);
		ConfigureChrome();
		Build();
	}

	public void Sync(IReadOnlyList<ActiveObjective> objectives)
	{
		var signature = BuildSignature(objectives);
		if (signature == _lastSignature)
			return;

		_lastSignature = signature;
		RebuildEntries(objectives);
	}

	private void ConfigureChrome()
	{
		SetAnchorsPreset(LayoutPreset.TopRight);
		AnchorLeft = 1f;
		AnchorRight = 1f;
		GrowHorizontal = GrowDirection.Begin;
		MouseFilter = MouseFilterEnum.Ignore;

		AddThemeConstantOverride("margin_top", 48);
		AddThemeConstantOverride("margin_right", HudStyles.Margin);
	}

	private void Build()
	{
		var shell = HudWidgets.CreateObjectivesPanel(_theme);
		shell.Root.CustomMinimumSize = new Vector2(PanelWidth, 0);
		AddChild(shell.Root);
		_headerCountLabel = shell.HeaderBadge!;

		_emptyLabel = new Label
		{
			Text = "No active objectives",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		HudThemes.StyleLabel(_emptyLabel, _theme, HudStyles.ObjectiveBodyLabelType);
		shell.Body.AddChild(_emptyLabel);

		_entriesHost = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_entriesHost.AddThemeConstantOverride("separation", HudStyles.ObjectivesEntryGap);
		shell.Body.AddChild(_entriesHost);
	}

	private void RebuildEntries(IReadOnlyList<ActiveObjective> objectives)
	{
		foreach (var child in _entriesHost.GetChildren())
			child.QueueFree();

		_emptyLabel.Visible = objectives.Count == 0;
		_entriesHost.Visible = objectives.Count > 0;
		_headerCountLabel.Text = $"{objectives.Count:D2}";

		for (var index = 0; index < objectives.Count; index++)
			_entriesHost.AddChild(WrapEntry(CreateEntry(objectives[index], index + 1)));
	}

	private Control WrapEntry(HBoxContainer row)
	{
		var panel = new PanelContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HudThemes.StylePanel(panel, _theme, HudStyles.ObjectiveEntryPanelType);
		panel.AddChild(row);
		return panel;
	}

	private HBoxContainer CreateEntry(ActiveObjective objective, int index)
	{
		var row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 8);

		var indexLabel = new Label
		{
			Text = $"{index:D2}",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudThemes.StyleLabel(indexLabel, _theme, HudStyles.ObjectiveIndexLabelType);
		row.AddChild(indexLabel);

		var title = new Label
		{
			Text = objective.Title.ToUpperInvariant(),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudThemes.StyleLabel(title, _theme, HudStyles.ObjectiveTitleLabelType);
		row.AddChild(title);

		var separator = new Label
		{
			Text = "//",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudThemes.StyleLabel(separator, _theme, HudStyles.ObjectiveSeparatorLabelType);
		row.AddChild(separator);

		var description = new Label
		{
			Text = objective.Summary,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HudThemes.StyleLabel(description, _theme, HudStyles.ObjectiveBodyLabelType);
		row.AddChild(description);

		return row;
	}

	private static string BuildSignature(IReadOnlyList<ActiveObjective> objectives)
	{
		if (objectives.Count == 0)
			return string.Empty;

		return string.Join(
			'\n',
			objectives.Select(objective => $"{objective.Source}:{objective.Id}:{objective.Title}:{objective.Summary}"));
	}
}
