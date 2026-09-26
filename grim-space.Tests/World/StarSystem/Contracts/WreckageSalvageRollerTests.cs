using GrimSpace.Math;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Encounter;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class WreckageSalvageRollerTests
{
	[Fact]
	public void RollSalvageLoot_TriplesShipScrapWithoutChangingOtherLoot()
	{
		const int mapSeed = 42;
		const string contractId = "wreck-loot";
		const EDangerLevel danger = EDangerLevel.Moderate;
		var tierSeed = StableSeedMixer.From(mapSeed).Add(contractId).Add("wreckage-salvage-tier").Value;
		var lootSeed = StableSeedMixer.From(mapSeed).Add(contractId).Add("wreckage-salvage-loot").Value;
		var wreck = ShipPowerCatalog.PickRandomWithinBudget(
			new StableRandom(tierSeed),
			EncounterPowerCatalog.EncounterPowerBudget(danger));
		var shipLoot = LootCatalog.SalvageFromShip(wreck, new StableRandom(lootSeed));

		var loot = WreckageSalvageRoller.RollSalvageLoot(mapSeed, contractId, danger);

		Assert.True(shipLoot.TryGet(ResourceId.ScrapAlloy, out var shipScrap));
		Assert.True(loot.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(shipScrap * 3, scrap);
		Assert.Equal(
			shipLoot.TryGet(ResourceId.IndustrialCore, out var shipCores) ? shipCores : 0,
			loot.TryGet(ResourceId.IndustrialCore, out var wreckCores) ? wreckCores : 0);
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
