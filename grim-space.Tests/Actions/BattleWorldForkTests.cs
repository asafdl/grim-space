using System.Collections.Frozen;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
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
		var asteroid = Asteroid.Create("asteroid", rock, grid, [rock]);
		Assert.Contains(rock, asteroid.AbilityBlockingCells);
		BattleTestWorld.InjectNonUnit(world, asteroid);

		var branch = world.Fork();
		var sibling = world.Fork();
		Assert.IsAssignableFrom<FrozenSet<Coord>>(asteroid.Cells);
		Assert.Contains(rock, (IReadOnlySet<Coord>)world.AbilityBlockingCells);
		Assert.NotSame(world.NonUnits, branch.NonUnits);
		Assert.Same(asteroid, branch.NonUnits[asteroid.Id]);
		Assert.Same(asteroid, sibling.NonUnits[asteroid.Id]);
		Assert.Same(world.AbilityBlockingCells, branch.AbilityBlockingCells);
		Assert.Same(world.AbilityBlockingCells, sibling.AbilityBlockingCells);
		Assert.True(branch.RemoveNonUnit(asteroid.Id));
		Assert.Contains(asteroid.Id, world.NonUnits.Keys);
		Assert.Contains(asteroid.Id, sibling.NonUnits.Keys);
		Assert.DoesNotContain(rock, (IReadOnlySet<Coord>)branch.AbilityBlockingCells);
		Assert.Contains(rock, (IReadOnlySet<Coord>)world.AbilityBlockingCells);
		Assert.Contains(rock, (IReadOnlySet<Coord>)sibling.AbilityBlockingCells);
		Assert.NotSame(world.AbilityBlockingCells, branch.AbilityBlockingCells);

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
	public void ImmutableGoopHazardIsSharedAcrossForks()
	{
		var origin = new Coord(5, 5, 5);
		var world = BattleTestFixture.BeginSimulation(origin).Engine.World;
		var hazard = new GoopHazard
		{
			Id = "mutable-goop",
			ActorId = "player",
			Center = origin,
			Frame = BodyFrame.WorldAligned(origin),
			Cells = new HashSet<Coord> { origin }.ToFrozenSet(),
		};
		BattleTestWorld.InjectNonUnit(world, hazard);

		var fork = world.Fork();
		var copy = Assert.IsType<GoopHazard>(fork.NonUnits[hazard.Id]);
		Assert.Contains(origin, copy.AbilityBlockingCells);
		Assert.Same(hazard, copy);
		Assert.Same(world.AbilityBlockingCells, fork.AbilityBlockingCells);
	}

	[Fact]
	public void SpatialQueriesTrackOverlappingHazardsAcrossForkMutations()
	{
		var center = new Coord(5, 5, 5);
		var cell = center + new Coord(1, 0, 0);
		using var battle = BattleTestFixture.BeginSimulation(center);
		var world = battle.Engine.World;
		var well = GravityWell.Create("gravitywell-test", center, BodyFrame.WorldAligned(center), world.Grid);
		var firstPlume = CryoPlume.Create("cryoplume-first", cell, world.Grid);
		var secondPlume = CryoPlume.Create("cryoplume-second", cell, world.Grid);

		Assert.Null(world.CryoPlumeAt(cell));
		Assert.False(world.IsTranslationAssisted(cell, Coord.Forward));
		world.AddNonUnit(well);
		world.AddNonUnit(firstPlume);
		world.AddNonUnit(secondPlume);
		var branch = world.Fork();

		Assert.Same(firstPlume, branch.CryoPlumeAt(cell));
		Assert.True(branch.IsTranslationAssisted(cell, Coord.Forward));
		Assert.False(branch.IsTranslationAssisted(center, Coord.Forward));
		Assert.False(branch.IsTranslationAssisted(cell, -Coord.Forward));

		Assert.True(branch.RemoveNonUnit(firstPlume.Id));
		Assert.Same(secondPlume, branch.CryoPlumeAt(cell));
		Assert.True(branch.IsTranslationAssisted(cell, Coord.Forward));
		Assert.True(branch.RemoveNonUnit(well.Id));
		Assert.False(branch.IsTranslationAssisted(cell, Coord.Forward));
		Assert.Same(secondPlume, branch.CryoPlumeAt(cell));
		Assert.Same(firstPlume, world.CryoPlumeAt(cell));
		Assert.True(world.IsTranslationAssisted(cell, Coord.Forward));

		branch.AddNonUnit(well);
		Assert.True(branch.IsTranslationAssisted(cell, Coord.Forward));
		Assert.True(world.RemoveNonUnit(secondPlume.Id));
		Assert.Same(secondPlume, branch.CryoPlumeAt(cell));
	}
}
