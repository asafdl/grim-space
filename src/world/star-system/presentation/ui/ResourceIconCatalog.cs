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
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};

	public static string IconPath(ResourceId id) =>
		id switch
		{
			ResourceId.Credits => "res://assets/ui/resources/credits.svg",
			ResourceId.ScrapAlloy => "res://assets/ui/resources/scrap-alloy.svg",
			ResourceId.IndustrialCore => "res://assets/ui/resources/industrial-core.svg",
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};
}
