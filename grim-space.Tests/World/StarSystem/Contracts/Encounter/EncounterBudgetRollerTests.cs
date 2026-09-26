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
	public void Roll_VeryLow_IncludesPatrol()
	{
		var ships = EncounterBudgetRoller.Roll(42, "tutorial-beat-a-hunt-42", "hunt-encounter", EDangerLevel.VeryLow);

		Assert.Contains(ships, ship => ship.Chassis == EType.Patrol);
	}
}
