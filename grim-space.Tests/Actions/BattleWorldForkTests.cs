using System.Collections.Frozen;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class BattleWorldForkTests
{
	[Fact]
	public void AsteroidIsSharedButUnitsTimelineAndMembershipStayIndependent()
	{
		var origin = new Coord(5, 5, 5);
		var rock = origin + Coord.Up;
		var grid = BattleTestFixture.Grid();
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(10, 5, 5)),
			grid,
			new HashSet<Coord> { rock });
		var world = battle.Engine.World;
		var asteroid = Hazard.Asteroid("asteroid", rock, grid, [rock]);
		BattleTestWorld.InjectHazard(world, asteroid);

		var branch = world.Fork();
		var sibling = world.Fork();
		Assert.IsAssignableFrom<FrozenSet<Coord>>(asteroid.Cells);
		Assert.NotSame(world.NonUnits, branch.NonUnits);
		Assert.Same(asteroid, branch.NonUnits[asteroid.Id]);
		Assert.Same(asteroid, sibling.NonUnits[asteroid.Id]);
		branch.MutableNonUnits.Remove(asteroid.Id);
		Assert.Contains(asteroid.Id, world.NonUnits.Keys);
		Assert.Contains(asteroid.Id, sibling.NonUnits.Keys);

		branch.StateOf(battle.PlayerId).Position += Coord.Forward;
		branch.Timeline.Schedule(0, new MoveStepAction(battle.PlayerId));
		Assert.Equal(origin, world.StateOf(battle.PlayerId).Position);
		Assert.Equal(origin, sibling.StateOf(battle.PlayerId).Position);
		Assert.Empty(world.Timeline.TakePending());
		Assert.Single(branch.Timeline.TakePending());

		var sim = battle.Engine.CreateSimulation();
		Assert.False(sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Dorsal)));
		Assert.True(sim.TryEnqueue(new MoveStepAction(battle.PlayerId)));
		var saved = sim.Fork();
		sim.Dequeue(0);
		Assert.Equal(origin, sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.Equal(origin + Coord.Forward, saved.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.Equal(origin, world.StateOf(battle.PlayerId).Position);
	}

	[Fact]
	public void MutableHazardStillClonesAcrossForks()
	{
		var origin = new Coord(5, 5, 5);
		var world = BattleTestFixture.BeginSimulation(origin).Engine.World;
		var hazard = HazardResolution.BuildTransient(
			EHazardKind.ScrapDroneSwarmBurst, [origin], 1, origin);
		BattleTestWorld.InjectHazard(world, hazard);

		var fork = world.Fork();
		var copy = Assert.IsType<Hazard>(fork.NonUnits[hazard.Id]);
		Assert.NotSame(hazard, copy);
		((HashSet<Coord>)copy.Cells).Add(origin + Coord.Up);
		Assert.DoesNotContain(origin + Coord.Up, hazard.Cells);
	}
}
