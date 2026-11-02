using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Contracts.Encounter;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Encounter;

[StarSystemTestSuite]
public sealed class EncounterBudgetRollerTests
{
	[Fact]
	public void Roll_SameSeed_ProducesSameShips()
	{
		var first = EncounterBudgetRoller.Roll(42, "contract-a", "hunt-encounter", EDangerLevel.Moderate);
		var second = EncounterBudgetRoller.Roll(42, "contract-a", "hunt-encounter", EDangerLevel.Moderate);

		Assert.Equal(first, second);
	}

	[Fact]
	public void Roll_DifferentSeeds_CanDiffer()
	{
		var a = EncounterBudgetRoller.Roll(1, "contract-a", "hunt-encounter", EDangerLevel.High);
		var b = EncounterBudgetRoller.Roll(2, "contract-a", "hunt-encounter", EDangerLevel.High);

		Assert.NotEqual(a, b);
	}

	[Fact]
	public void Roll_RespectsPowerBudget()
	{
		var ships = EncounterBudgetRoller.Roll(99, "budget-test", "hunt-encounter", EDangerLevel.Low);
		var total = ships.Sum(ship =>
			ShipPowerCatalog.All
				.First(entry => entry.Chassis == ship.Chassis && entry.Tier == ship.GearTier)
				.Power);

		Assert.InRange(total, 1, EncounterPowerCatalog.EncounterPowerBudget(EDangerLevel.Low));
	}

	[Fact]
	public void Roll_VeryLow_IncludesRepurposedMiner()
	{
		var ships = EncounterBudgetRoller.Roll(42, "tutorial-beat-a-hunt-42", "hunt-encounter", EDangerLevel.VeryLow);

		Assert.Contains(ships, ship => ship.Chassis == EType.RepurposedMiner);
	}

	[Fact]
	public void Roll_Moderate_PrefersMoreCheapEnemiesThanUniform()
	{
		const int mapSeed = 42;
		const string scope = "hunt-encounter";
		const int rolls = 200;
		var biasedCount = 0;
		var biasedHeavyMiners = 0;
		var uniformHeavyMiners = 0;
		for (var i = 0; i < rolls; i++)
		{
			var contractId = $"c{i}";
			var biased = EncounterBudgetRoller.Roll(mapSeed, contractId, scope, EDangerLevel.Moderate);
			var uniform = EncounterBudgetRoller.Roll(
				mapSeed,
				contractId,
				scope,
				EDangerLevel.Moderate,
				cheapBiasExponent: 0);
			biasedCount += biased.Count;
			if (HasTier2Miner(biased))
				biasedHeavyMiners++;
			if (HasTier2Miner(uniform))
				uniformHeavyMiners++;
		}

		Assert.True(biasedCount / (double)rolls >= 3);
		Assert.True(biasedHeavyMiners < uniformHeavyMiners);
	}

	[Fact]
	public void Roll_VeryHigh_CapsEnemyCount()
	{
		for (var i = 0; i < 200; i++)
		{
			var ships = EncounterBudgetRoller.Roll(42, $"c{i}", "hunt-encounter", EDangerLevel.VeryHigh);
			Assert.InRange(ships.Count, 1, EncounterBudgetRoller.MaxEnemies);
		}
	}

	private static bool HasTier2Miner(IReadOnlyList<(EType Chassis, EShipGearTier GearTier)> ships) =>
		ships.Any(ship => ship.Chassis == EType.RepurposedMiner && ship.GearTier >= EShipGearTier.T2);
}
