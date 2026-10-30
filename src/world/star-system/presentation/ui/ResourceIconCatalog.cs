using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public static class ResourceIconCatalog
{
	public static string DisplayName(ResourceId id) =>
		id switch
		{
			ResourceId.Credits => "Credits",
			ResourceId.ScrapAlloy => "Scrap Alloy",
			ResourceId.IndustrialCore => "Industrial Core",
			ResourceId.Fuel => "Fuel",
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};

	public static HudTooltipContent Tooltip(ResourceId id) =>
		id switch
		{
			ResourceId.Fuel => new HudTooltipContent(
				"Fuel",
				"When out of fuel, your ships will start taking damage, as they can no longer manuever properly."),
			_ => new HudTooltipContent(DisplayName(id), ""),
		};

	public static string IconPath(ResourceId id) =>
		id switch
		{
			ResourceId.Credits => "res://assets/ui/resources/credits.svg",
			ResourceId.ScrapAlloy => "res://assets/ui/resources/scrap-alloy.svg",
			ResourceId.IndustrialCore => "res://assets/ui/resources/industrial-core.svg",
			ResourceId.Fuel => "res://assets/ui/resources/fuel.png",
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};

	public static Color RailColor(ResourceId id) =>
		id switch
		{
			ResourceId.Credits => new Color(0.95f, 0.76f, 0.25f),
			ResourceId.ScrapAlloy => new Color(0.62f, 0.68f, 0.78f),
			ResourceId.IndustrialCore => new Color(0.95f, 0.5f, 0.22f),
			ResourceId.Fuel => new Color(0.3f, 0.82f, 0.62f),
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};
}
