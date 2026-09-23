using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Poi;

[StarSystemTestSuite]
public sealed class OperatorNamesTests
{
	[Fact]
	public void Pool_HasNoDuplicateNames_CaseInsensitive()
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var name in OperatorNames.Pool)
			Assert.True(seen.Add(name), $"Duplicate operator name in pool: {name}");
	}
}
