using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Tests.Pilots;

public sealed class RobotNameCatalogTests
{
	[Fact]
	public void All_IncludesLegacyOperatorConstants()
	{
		Assert.Contains(OperatorNames.Effy, RobotNameCatalog.All);
		Assert.Contains(OperatorNames.GiZmo, RobotNameCatalog.All);
		Assert.Contains(OperatorNames.K0rtex, RobotNameCatalog.All);
	}

	[Fact]
	public void Roll_SkipsUsedNames()
	{
		var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EFFY" };
		var rolled = RobotNameCatalog.Roll(new Random(1), used);
		Assert.False(string.Equals(rolled, "EFFY", StringComparison.OrdinalIgnoreCase));
		Assert.True(
			RobotNameCatalog.All.Contains(rolled)
			|| RobotNameCatalog.All.Any(name =>
				rolled.StartsWith(name + "-", StringComparison.OrdinalIgnoreCase)));
	}
}
