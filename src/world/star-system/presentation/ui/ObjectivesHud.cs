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
	private readonly Dictionary<string, PanelContainer> _entries = new(StringComparer.Ordinal);
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

	public void NotifyAccepted(string contractId)
	{
		if (!_entries.TryGetValue(contractId, out var panel))
		{
			GD.PushError($"Accepted contract '{contractId}' has no objective entry.");
			return;
		}

		var badge = new Label
		{
			Text = "NEW",
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.InformativeItemTitleLabelType,
		};
		badge.AddThemeColorOverride("font_color", new Color(0.45f, 0.95f, 0.55f));
		((HBoxContainer)panel.GetChild(0)).AddChild(badge);
		var tween = CreateTween();
		tween.TweenInterval(4f);
		tween.TweenProperty(badge, "modulate:a", 0f, 1.5f);
		tween.Finished += badge.QueueFree;
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
		_entries.Clear();

		_emptyLabel.Visible = objectives.Count == 0;
		_entriesHost.Visible = objectives.Count > 0;
		_headerCountLabel.Text = $"{objectives.Count:D2}";

		for (var index = 0; index < objectives.Count; index++)
		{
			var panel = WrapEntry(CreateEntry(objectives[index], index + 1));
			_entries[objectives[index].Id] = panel;
			_entriesHost.AddChild(panel);
		}
	}

	private PanelContainer WrapEntry(Control content)
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
			Text = objective.Summary.ToBbcode(),
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
			ObjectiveSummaryContent.NearLandmark near =>
				$"near:{near.Prefix}{near.LandmarkPoiId}:{near.LandmarkDisplayName}{near.Suffix}",
			ObjectiveSummaryContent.RouteBetweenLandmarks route =>
				$"route:{route.Prefix}" +
				$"{route.LandmarkAPoiId}:{route.LandmarkADisplayName}" +
				$"{route.Connector}" +
				$"{route.LandmarkBPoiId}:{route.LandmarkBDisplayName}" +
				$"{route.Suffix}",
			ObjectiveSummaryContent.RouteAmongLandmarks route =>
				$"triangle:{route.Prefix}" +
				$"{route.LandmarkAPoiId}:{route.LandmarkADisplayName}" +
				$"{route.ConnectorAB}" +
				$"{route.LandmarkBPoiId}:{route.LandmarkBDisplayName}" +
				$"{route.ConnectorBC}" +
				$"{route.LandmarkCPoiId}:{route.LandmarkCDisplayName}" +
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
