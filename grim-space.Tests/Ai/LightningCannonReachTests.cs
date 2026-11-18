using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class LightningCannonReachTests
{
	[Fact]
	public void CouldPossiblyDamage_WhenWithinWeaponReachAlone_ReturnsTrue()
	{
		var self = Coord.Zero;
		var reach = CatalogExpectations.LightningCannonMaxReach();
		var opponent = new Coord(reach, 0, 0);

		Assert.True(OffensiveReach.CouldPossiblyDamage(self, actionPoints: 0, opponent, reach));
	}

	[Fact]
	public void CouldPossiblyDamage_WhenJustBeyondReachAndMoveBubble_ReturnsFalse()
	{
		var ap = 2;
		var reach = CatalogExpectations.LightningCannonMaxReach();
		var bubble = OffensiveReach.OptimisticMoveBubble(ap);
		var self = Coord.Zero;
		var opponent = new Coord(bubble + reach + 1, 0, 0);

		Assert.False(OffensiveReach.CouldPossiblyDamage(self, ap, opponent, reach));
	}

	[Fact]
	public void CouldPossiblyDamage_WhenMoveBubbleClosesTheGap_ReturnsTrue()
	{
		var ap = 5;
		var reach = CatalogExpectations.LightningCannonMaxReach();
		var bubble = OffensiveReach.OptimisticMoveBubble(ap);
		var self = Coord.Zero;
		var opponent = new Coord(bubble + reach, 0, 0);

		Assert.True(OffensiveReach.CouldPossiblyDamage(self, ap, opponent, reach));
	}

	[Fact]
	public void OptimisticMoveBubble_EqualsAvailableActionPoints()
	{
		Assert.Equal(4, OffensiveReach.OptimisticMoveBubble(4));
	}

	[Fact]
	public void UpperBound_OnlyAllowsFacing_WhenOutOfReachWithNoAp()
	{
		var ap = 0;
		var enemy = CreateUnit(ETeam.Enemy, "enemy", new Coord(0, 5, 5), EType.Carrier);
		enemy.State.CurrentActionPoints = ap;
		var gap = OffensiveReach.OptimisticMoveBubble(ap) + MaxPerTurnWeaponReach(enemy.State) + 1;
		var player = CreateUnit(ETeam.Player, "player", new Coord(gap, 5, 5), EType.Fighter);

		var battle = BattleTestFixture.BeginSimulation(player, enemy, BattleTestFixture.Grid(size: 32));
		var simulation = (BattleSimulation)battle.Engine.CreateSimulation();
		var bound = EnemySearchInput.Evaluate(simulation, simulation, enemy.State.Id).UpperBound;

		Assert.Equal(
			EnemySearchInput.FacingWeight,
			bound);
	}

	[Fact]
	public void UpperBound_IncludesDamageBonus_WhenOpponentInOptimisticReach()
	{
		var player = CreateUnit(ETeam.Player, "player", new Coord(6, 5, 5), EType.Fighter);
		var enemy = CreateUnit(ETeam.Enemy, "enemy", new Coord(0, 5, 5), EType.Carrier);
		enemy.State.CurrentActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var simulation = (BattleSimulation)battle.Engine.CreateSimulation();
		var bound = EnemySearchInput.Evaluate(simulation, simulation, enemy.State.Id).UpperBound;

		Assert.Equal(
			EnemySearchInput.DamageHitBonus,
			bound);
	}

	[Fact]
	public void UpperBound_IncludesBothScrapDroneSwarmUses()
	{
		var player = CreateUnit(ETeam.Player, "player", new Coord(4, 5, 5), EType.Fighter);
		var repurposedMiner = CreateUnit(ETeam.Enemy, "repurposedMiner", new Coord(0, 5, 5), EType.RepurposedMiner);
		repurposedMiner.State.CurrentActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, repurposedMiner);
		var simulation = (BattleSimulation)battle.Engine.CreateSimulation();
		var bound = EnemySearchInput.Evaluate(simulation, simulation, repurposedMiner.State.Id).UpperBound;

		Assert.Equal(
			2 * EnemySearchInput.DamageHitBonus,
			bound);
	}

	private static Unit CreateUnit(ETeam team, string id, Coord position, EType type) =>
		Factory.Create(
			ShipInstance.FromCatalog(id, type),
			team,
			position,
			new AiController(),
			new Coord(1, 0, 0),
			Coord.Up);

	private static int MaxPerTurnWeaponReach(State state)
	{
		var reach = 0;
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec is not IPerTurnAbility)
				continue;

			reach = System.Math.Max(reach, AbilityReach.MaxManhattanFromFirer(installed.Spec));
		}

		return reach;
	}
}
