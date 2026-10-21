using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class ResourceCostDisplayTests
{
	[Fact]
	public void FormatCompact_EmptyBundle_ReturnsFree()
	{
		Assert.Equal("FREE", ResourceCostDisplay.FormatCompact(ResourceBundle.Empty));
	}

	[Fact]
	public void FormatCompact_OrdersResources()
	{
		var text = ResourceCostDisplay.FormatCompact(ResourceBundle.Create(
			(ResourceId.ScrapAlloy, 12),
			(ResourceId.Credits, 90)));

		Assert.Equal("90 CR · 12 scrap", text);
	}
}
