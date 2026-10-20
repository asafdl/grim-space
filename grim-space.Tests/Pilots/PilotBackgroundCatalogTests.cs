using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotBackgroundCatalogTests
{
	[Fact]
	public void Get_ReturnsUnspecifiedMetadata()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.Unspecified);

		Assert.Equal(EPilotBackgroundId.Unspecified, definition.Id);
		Assert.Equal("Unspecified Background", definition.Name);
		Assert.Equal(
			"Temporary infrastructure placeholder. Real pilot backgrounds are not defined yet.",
			definition.Description);
	}

	[Fact]
	public void Get_ThrowsForUnknownBackground()
	{
		Assert.Throws<KeyNotFoundException>(() =>
			PilotBackgroundCatalog.Get((EPilotBackgroundId)999));
	}

	[Fact]
	public void All_EnumeratesRegisteredBackgrounds()
	{
		var backgrounds = PilotBackgroundCatalog.All.ToArray();

		Assert.Single(backgrounds);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.Unspecified);
	}
}
