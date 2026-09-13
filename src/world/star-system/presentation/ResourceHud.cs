using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class ResourceHud : MarginContainer
{
	private const int IconSize = 22;
	private const int EntrySeparation = 16;

	private readonly Dictionary<ResourceId, Label> _amountLabels = new();
	private string _lastSignature = "";

	public override void _Ready()
	{
		ConfigureChrome();
		Build();
	}

	public void Sync(PlayerResources resources)
	{
		var signature =
			$"{resources.GetBalance(ResourceId.Credits)}:" +
			$"{resources.GetBalance(ResourceId.ScrapAlloy)}:" +
			$"{resources.GetBalance(ResourceId.IndustrialCore)}";
		if (signature == _lastSignature)
			return;

		_lastSignature = signature;
		foreach (var (id, balance) in resources.EnumerateBalances())
			_amountLabels[id].Text = balance.ToString();
	}

	private void ConfigureChrome()
	{
		SetAnchorsPreset(LayoutPreset.TopRight);
		AnchorLeft = 1f;
		AnchorRight = 1f;
		GrowHorizontal = GrowDirection.Begin;
		MouseFilter = MouseFilterEnum.Ignore;

		AddThemeConstantOverride("margin_top", 48);
		AddThemeConstantOverride(
			"margin_right",
			HudStyles.Margin + HudStyles.ObjectivesHudPanelWidth + HudStyles.HudSiblingGap);
	}

	private void Build()
	{
		var shell = HudWidgets.CreateInformativePanel("RESOURCES");
		shell.Root.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		AddChild(shell.Root);

		var row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", EntrySeparation);
		shell.Body.AddChild(row);

		foreach (var id in Enum.GetValues<ResourceId>())
			row.AddChild(CreateEntry(id));
	}

	private Control CreateEntry(ResourceId id)
	{
		var entry = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Stop,
			TooltipText = DisplayName(id),
		};
		entry.AddThemeConstantOverride("separation", 6);

		var icon = new TextureRect
		{
			Texture = GD.Load<Texture2D>(IconPath(id)),
			CustomMinimumSize = new Vector2(IconSize, IconSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		entry.AddChild(icon);

		var amount = new Label
		{
			Text = "0",
			MouseFilter = MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = HudStyles.InformativeItemTitleLabelType,
		};
		entry.AddChild(amount);
		_amountLabels[id] = amount;

		return entry;
	}

	private static string DisplayName(ResourceId id) =>
		id switch
		{
			ResourceId.Credits => "Credits",
			ResourceId.ScrapAlloy => "Scrap Alloy",
			ResourceId.IndustrialCore => "Industrial Core",
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};

	private static string IconPath(ResourceId id) =>
		id switch
		{
			ResourceId.Credits => "res://assets/ui/resources/credits.svg",
			ResourceId.ScrapAlloy => "res://assets/ui/resources/scrap-alloy.svg",
			ResourceId.IndustrialCore => "res://assets/ui/resources/industrial-core.svg",
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};
}
