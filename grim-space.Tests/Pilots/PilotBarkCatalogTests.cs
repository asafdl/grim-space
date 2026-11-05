using GrimSpace.Pilots.Barks;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotBarkCatalogTests
{
	[Fact]
	public void EveryFlavor_HasNonEmptyLines()
	{
		foreach (var flavor in Enum.GetValues<EBarkFlavor>())
		{
			var lines = PilotBarkCatalog.LinesFor(flavor);
			Assert.NotEmpty(lines);
			Assert.All(lines, line => Assert.False(string.IsNullOrWhiteSpace(line)));
		}
	}

	[Fact]
	public void Roll_ReturnsLineFromFlavorPool()
	{
		var rng = new Random(7);

		var line = PilotBarkCatalog.Roll(EBarkFlavor.Angry, rng);

		Assert.Contains(line, PilotBarkCatalog.LinesFor(EBarkFlavor.Angry));
	}
}
