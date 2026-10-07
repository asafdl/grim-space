using GrimSpace.Battle;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class ScrapDroneSwarmActionTests
{
	private const string PlayerId = "player";

	private static int TotalShieldPoints(GrimSpace.Battle.Units.State state)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += state.ShieldPoints[face];
		return total;
	}

	[Fact]
	public void ScrapDroneSwarmAppliesResolveImmediately()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var swarm = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Port);

		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.ScrapDroneSwarm), StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId), EAbilityKind.ScrapDroneSwarm));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(swarm));
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.ScrapDroneSwarm) - 1, StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId), EAbilityKind.ScrapDroneSwarm));
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(swarm));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard)));
		Assert.Equal(0, StateMountTestKit.UsesRemaining(
			battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId),
			EAbilityKind.ScrapDroneSwarm));
	}

	[Fact]
	public void ScrapDroneSwarmLegalWhenBurstHasNoInBoundsCells()
	{
		var grid = BattleTestFixture.Grid(1);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(Coord.Zero),
			BattleTestFixture.Enemy(Coord.Zero),
			grid);
		var action = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Port);
		Assert.Empty(ScrapDroneSwarmDef.Instance.AffectedCells(action, battle.PlayerAgent.Sim.World));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));
	}

	[Fact]
	public void ScrapDroneSwarmAppliesDamageWithoutApPenalty()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var action = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard);
		var cells = ScrapDroneSwarmDef.Instance.AffectedCells(action, battle.PlayerAgent.Sim.World);
		var enemy = UnitRegistry.For(battle.PlayerAgent.Sim.World).All.First(unit => unit.State.Id != PlayerId);
		enemy.State.Position = cells.First();
		var shieldsBefore = TotalShieldPoints(enemy.State);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		Assert.Equal(shieldsBefore - CatalogExpectations.ScrapDroneSwarmDamage(), TotalShieldPoints(enemy.State));
		Assert.False(enemy.State.ApPenaltyNextTurn);
	}

	[Fact]
	public void AsteroidBetweenActorAndEnemyDoesNotSuppressSwarmDamage()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var grid = battle.PlayerAgent.Sim.World.Grid;
		var action = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard);
		var cells = ScrapDroneSwarmDef.Instance.AffectedCells(action, battle.PlayerAgent.Sim.World);
		var enemy = UnitRegistry.For(battle.PlayerAgent.Sim.World).All.First(unit => unit.State.Id != PlayerId);
		var targetCell = cells.First();
		enemy.State.Position = targetCell;
		var shooterPos = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).Position;
		var asteroidPos = new Coord(
			(shooterPos.X + targetCell.X) / 2,
			(shooterPos.Y + targetCell.Y) / 2,
			(shooterPos.Z + targetCell.Z) / 2);
		BattleTestWorld.InjectNonUnit(
			battle.PlayerAgent.Sim.World,
			Asteroid.Create("asteroid", asteroidPos, grid, [asteroidPos]));
		var shieldsBefore = TotalShieldPoints(enemy.State);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		Assert.Equal(shieldsBefore - CatalogExpectations.ScrapDroneSwarmDamage(), TotalShieldPoints(enemy.State));
	}
}
