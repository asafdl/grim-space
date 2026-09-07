using Godot;
using GrimSpace.Presentation.Ui.Hud;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class ObjectivesHud : MarginContainer
{
	private const int PanelWidth = 300;

	private VBoxContainer _entriesHost = null!;
	private Label _emptyLabel = null!;
	private string _lastSignature = "";

	public override void _Ready()
	{
		HudThemes.Apply(this, HudThemeFamily.Informative);
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
		var shell = HudWidgets.CreateHudPanel("Objectives", HudThemeFamily.Informative);
		shell.Root.CustomMinimumSize = new Vector2(PanelWidth, 0);
		AddChild(shell.Root);

		_entriesHost = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_entriesHost.AddThemeConstantOverride("separation", 8);
		shell.Body.AddChild(_entriesHost);

		_emptyLabel = new Label
		{
			Text = "No active objectives",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.EntryBodyVariation(HudThemeFamily.Informative),
		};
		shell.Body.AddChild(_emptyLabel);
	}

	private void RebuildEntries(IReadOnlyList<ActiveObjective> objectives)
	{
		foreach (var child in _entriesHost.GetChildren())
			child.QueueFree();

		_emptyLabel.Visible = objectives.Count == 0;
		_entriesHost.Visible = objectives.Count > 0;

		foreach (var objective in objectives)
			_entriesHost.AddChild(CreateEntry(objective));
	}

	private static Control CreateEntry(ActiveObjective objective)
	{
		var column = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		column.AddThemeConstantOverride("separation", 2);

		var title = new Label
		{
			Text = objective.Title,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.EntryTitleVariation(HudThemeFamily.Informative),
		};
		column.AddChild(title);

		var summary = new Label
		{
			Text = objective.Summary,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.EntryBodyVariation(HudThemeFamily.Informative),
		};
		column.AddChild(summary);

		return column;
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
