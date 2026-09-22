using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ObjectivesHud : MarginContainer
{
	private const int ObjectiveTitleFontSize = 18;
	private const int ObjectiveSummaryFontSize = 16;

	private VBoxContainer _entriesHost = null!;
	private Label _emptyLabel = null!;
	private Label _headerCountLabel = null!;
	private string _lastSignature = "";

	public event Action<string>? LandmarkLinkClicked;

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
		shell.Root.CustomMinimumSize = new Vector2(HudStyles.ObjectivesHudPanelWidth, 0);
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

	private Control WrapEntry(Control content)
	{
		var panel = new PanelContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeListItemPanelType,
		};
		panel.AddChild(content);
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

		var details = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		var title = new Label
		{
			Text = objective.Title.ToUpperInvariant(),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeItemTitleLabelType,
		};
		title.AddThemeFontSizeOverride("font_size", ObjectiveTitleFontSize);
		details.AddChild(title);

		var summary = new RichTextLabel
		{
			BbcodeEnabled = true,
			Text = ObjectiveSummaryFormatter.ToBbcode(objective.Summary),
			FitContent = true,
			ScrollActive = false,
			SelectionEnabled = false,
			ContextMenuEnabled = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Stop,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeItemDescriptionRichTextLabelType,
			MetaUnderlined = true,
		};
		summary.AddThemeFontSizeOverride("normal_font_size", ObjectiveSummaryFontSize);
		summary.MetaClicked += metadata => OnSummaryMetaClicked(metadata);
		details.AddChild(summary);
		row.AddChild(details);

		if (objective.Source == EObjectiveSource.Contract && !objective.Reward.IsEmpty)
			row.AddChild(ResourceRewardDisplay.CreateCompact(objective.Reward));

		return row;
	}

	private void OnSummaryMetaClicked(Variant metadata)
	{
		if (metadata.VariantType != Variant.Type.String)
		{
			GD.PushError($"ObjectivesHud received unsupported link metadata type '{metadata.VariantType}'.");
			return;
		}

		LandmarkLinkClicked?.Invoke(metadata.AsString());
	}

	private static string BuildSignature(IReadOnlyList<ActiveObjective> objectives)
	{
		if (objectives.Count == 0)
			return string.Empty;

		return string.Join(
			'\n',
			objectives.Select(FormatObjectiveSignature));
	}

	private static string FormatObjectiveSignature(ActiveObjective objective)
	{
		var summarySignature = objective.Summary switch
		{
			ObjectiveSummaryContent.Plain plain => $"plain:{plain.Text}",
			ObjectiveSummaryContent.RouteBetweenLandmarks route =>
				$"route:{route.Prefix}" +
				$"{route.LandmarkAPoiId}:{route.LandmarkADisplayName}" +
				$"{route.Connector}" +
				$"{route.LandmarkBPoiId}:{route.LandmarkBDisplayName}" +
				$"{route.Suffix}",
			_ => "unknown",
		};
		var rewardSignature = string.Join(
			',',
			objective.Reward
				.OrderBy(entry => entry.Key)
				.Select(entry => $"{entry.Key}:{entry.Value}"));
		return $"{objective.Source}:{objective.Id}:{objective.Title}:{summarySignature}:{rewardSignature}";
	}
}
