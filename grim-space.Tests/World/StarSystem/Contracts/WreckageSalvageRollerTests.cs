using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class WreckageSalvageRollerTests
{
	[Fact]
	public void RollSalvageLoot_UsesLootCatalogScale()
	{
		var loot = WreckageSalvageRoller.RollSalvageLoot(42, "wreck-loot", EDangerLevel.Moderate);

		Assert.True(loot.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.InRange(scrap, 50, 400);
	}

	[Fact]
	public void RollSalvageLoot_SameInputs_IsDeterministic()
	{
		var first = WreckageSalvageRoller.RollSalvageLoot(42, "wreck-a", EDangerLevel.VeryLow);
		var second = WreckageSalvageRoller.RollSalvageLoot(42, "wreck-a", EDangerLevel.VeryLow);

		Assert.Equal(first, second);
	}

	[Fact]
	public void RollSalvageOutcome_SameInputs_IsDeterministic()
	{
		var first = WreckageSalvageRoller.RollSalvageOutcome(42, "wreck-b");
		var second = WreckageSalvageRoller.RollSalvageOutcome(42, "wreck-b");

		Assert.Equal(first, second);
	}
}
