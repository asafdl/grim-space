using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation;

public static class ResourceRewardDisplay
{
	private const int IconSize = 16;
	private const int AmountMinimumWidth = 40;

	public static Control CreateCompact(ResourceBundle reward)
	{
		var column = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		column.AddThemeConstantOverride("separation", 4);

		foreach (var (id, amount) in reward.OrderBy(entry => entry.Key))
			column.AddChild(CreateEntry(id, amount));

		return column;
	}

	private static Control CreateEntry(ResourceId id, int amount)
	{
		var row = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
		};
		row.AddThemeConstantOverride("separation", 4);

		row.AddChild(new TextureRect
		{
			Texture = GD.Load<Texture2D>(IconPath(id)),
			CustomMinimumSize = new Vector2(IconSize, IconSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			TooltipText = DisplayName(id),
		});

		var amountLabel = new Label
		{
			Text = amount.ToString(),
			CustomMinimumSize = new Vector2(AmountMinimumWidth, IconSize),
			HorizontalAlignment = HorizontalAlignment.Right,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
		};
		HudStyles.ApplyTextRole(amountLabel, HudTextRole.Success);
		row.AddChild(amountLabel);

		return row;
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
