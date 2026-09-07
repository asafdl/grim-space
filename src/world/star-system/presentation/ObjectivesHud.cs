using Godot;
using GrimSpace.Presentation.Ui.Hud;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class ObjectivesHud : MarginContainer
{
	private const int PanelWidth = 500;

	private VBoxContainer _entriesHost = null!;
	private Label _emptyLabel = null!;
	private Label _headerCountLabel = null!;
	private string _lastSignature = "";

	public override void _Ready()
	{
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
		var shell = HudWidgets.CreateInformativeListPanel(
			"ACTIVE OBJECTIVES",
			HudStyles.ObjectivesHudPanelType,
			includeCounter: true);
		shell.Root.CustomMinimumSize = new Vector2(PanelWidth, 0);
		AddChild(shell.Root);
		_headerCountLabel = shell.HeaderBadge!;

		_emptyLabel = new Label
		{
			Text = "No active objectives",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeItemDescriptionLabelType,
		};
		shell.Body.AddChild(_emptyLabel);

		_entriesHost = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeListVBoxType,
		};
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
			ThemeTypeVariation = HudStyles.InformativeListItemPanelType,
		};
		panel.AddChild(row);
		return panel;
	}

	private HBoxContainer CreateEntry(ActiveObjective objective, int index)
	{
		var row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeListItemHBoxType,
		};

		row.AddChild(new Label
		{
			Text = $"{index:D2}",
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.InformativeItemIndexLabelType,
		});

		row.AddChild(new Label
		{
			Text = objective.Title.ToUpperInvariant(),
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.InformativeItemTitleLabelType,
		});

		row.AddChild(new Label
		{
			Text = "//",
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.InformativeItemSeparatorLabelType,
		});

		row.AddChild(new Label
		{
			Text = objective.Summary,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeItemDescriptionLabelType,
		});

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
