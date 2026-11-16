using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Maneuvering;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests.Movement;

[BattleTestSuite]
public sealed class GravityWellMovementTests
{
	private const string PlayerId = "player";
	private static readonly Coord Center = new(6, 6, 6);
	private static readonly Coord Right = new(1, 0, 0);

	[Theory]
	[InlineData(1, ESpatialOrientation.Forward, true)]
	[InlineData(1, ESpatialOrientation.Retro, false)]
	[InlineData(-1, ESpatialOrientation.Retro, true)]
	[InlineData(-1, ESpatialOrientation.Forward, false)]
	public void MirroredSidesDiscountOnlyTheirHeadings(int side, ESpatialOrientation direction, bool assisted)
	{
		using var battle = CreateBattle(BattleTestFixture.Player(Center + Right * side));
		var sim = battle.Engine.CreateSimulation();
		var before = sim.World.StateOf(PlayerId).CurrentActionPoints;
		var normalCost = direction == ESpatialOrientation.Retro ? 2 : 1;

		Assert.True(sim.TryEnqueue(new MoveStepAction(PlayerId, direction)));
		var state = sim.World.StateOf(PlayerId);
		var cost = assisted ? 0 : normalCost;
		Assert.Equal(before - cost, state.CurrentActionPoints);
		Assert.Equal(HeatChangeEffect.PerApSpent * cost, state.Heat);
	}

	[Fact]
	public void EnteringCostsNormallyButConsecutiveStepsAndExitUseSourceCell()
	{
		var origin = Center + Right - Coord.Forward * 2;
		using var battle = CreateBattle(BattleTestFixture.Player(origin));
		var sim = battle.Engine.CreateSimulation();
		var state = sim.World.StateOf(PlayerId);
		var initialMp = state.CurrentManeuverPoints;

		Assert.True(sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.Equal(3, state.CurrentActionPoints);
		Assert.Equal(HeatChangeEffect.PerApSpent, state.Heat);
		for (var i = 0; i < 3; i++)
			Assert.True(sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.Equal(Center + Right + Coord.Forward * 2, state.Position);
		Assert.Equal(3, state.CurrentActionPoints);
		Assert.Equal(HeatChangeEffect.PerApSpent, state.Heat);
		Assert.Equal(initialMp, state.CurrentManeuverPoints);

		Assert.True(sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.Equal(2, state.CurrentActionPoints);
		Assert.Equal(2 * HeatChangeEffect.PerApSpent, state.Heat);
	}

	[Fact]
	public void RotatedShipUsesWorldHeadingAndCanMoveWithZeroAp()
	{
		var player = BattleTestFixture.Player(Center + Right, actionPoints: 0);
		player.State.Fore = Right;
		player.State.Dorsal = Coord.Up;
		player.State.Starboard = -Coord.Forward;
		using var battle = CreateBattle(player);
		var sim = battle.Engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.True(sim.TryEnqueue(new MoveStepAction(PlayerId, ESpatialOrientation.Port)));
		var state = sim.World.StateOf(PlayerId);
		Assert.Equal(Center + Right + Coord.Forward, state.Position);
		Assert.Equal(0, state.CurrentActionPoints);
		Assert.Equal(0, state.Heat);
	}

	[Fact]
	public void RotatedWellAssistsItsRotatedWorldHeadings()
	{
		var player = BattleTestFixture.Player(Center - Coord.Forward, actionPoints: 0);
		player.State.Fore = Right;
		player.State.Dorsal = Coord.Up;
		player.State.Starboard = -Coord.Forward;
		var wellFrame = new BodyFrame(Center, Right, Coord.Up, -Coord.Forward);
		using var battle = CreateBattle(player, frame: wellFrame);
		var sim = battle.Engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.Equal(Center - Coord.Forward + Right, sim.World.StateOf(PlayerId).Position);
		Assert.Equal(0, sim.World.StateOf(PlayerId).CurrentActionPoints);
	}

	[Fact]
	public void AssistanceDoesNotOverrideUnsupportedBlockedOrOutOfBoundsMoves()
	{
		var player = BattleTestFixture.Player(Center + Right, actionPoints: 0);
		player.State.Maneuverability = new ManeuverabilitySpec(
			4, 3,
			new Dictionary<ESpatialOrientation, int> { [ESpatialOrientation.Starboard] = 1 },
			new Dictionary<EHeadingTurn, int>(),
			new Dictionary<ERollDirection, int>());
		using var unsupported = CreateBattle(player);
		var unsupportedSim = unsupported.Engine.CreateSimulation();
		Assert.True(unsupportedSim.World.IsTranslationAssisted(player.State.Position, Coord.Forward));
		Assert.False(unsupportedSim.TryEnqueue(new MoveStepAction(PlayerId)));

		using var blocked = CreateBattle(BattleTestFixture.Player(Center - Coord.Forward, actionPoints: 0));
		Assert.False(blocked.Engine.CreateSimulation().TryEnqueue(new MoveStepAction(PlayerId)));

		var source = Center + Right + Coord.Forward;
		var grid = BattleTestFixture.Grid(16);
		var asteroidCell = source + Coord.Forward;
		var asteroid = Asteroid.Create("asteroid-test", asteroidCell, grid, [asteroidCell]);
		using var blockedExit = CreateBattle(
			BattleTestFixture.Player(source, actionPoints: 0),
			extraHazards: [asteroid],
			grid: grid);
		var blockedSim = blockedExit.Engine.CreateSimulation();
		Assert.True(blockedSim.World.IsTranslationAssisted(source, Coord.Forward));
		Assert.False(blockedSim.TryEnqueue(new MoveStepAction(PlayerId)));

		var edgeCenter = new Coord(1, 6, 6);
		using var edge = CreateBattle(
			BattleTestFixture.Player(edgeCenter - Right + Coord.Forward, actionPoints: 0),
			edgeCenter);
		var edgeSim = edge.Engine.CreateSimulation();
		Assert.True(edgeSim.World.IsTranslationAssisted(edgeCenter - Right + Coord.Forward, -Right));
		Assert.False(edgeSim.TryEnqueue(new MoveStepAction(PlayerId, ESpatialOrientation.Port)));
	}

	[Fact]
	public void FreeMoveStillTriggersTerrainAndSimulationCommitAndUndoAgree()
	{
		var origin = Center + Right;
		var destination = origin + Coord.Forward;
		var player = BattleTestFixture.Player(origin, actionPoints: 0);
		player.State.Heat = 80;
		var grid = BattleTestFixture.Grid(16);
		var plume = CryoPlume.Create("cryoplume-test", destination, grid);
		using var battle = CreateBattle(player, extraHazards: [plume], grid: grid);
		var sim = battle.Engine.CreateSimulation();
		var action = new MoveStepAction(PlayerId);
		var initialMp = sim.World.StateOf(PlayerId).CurrentManeuverPoints;

		Assert.True(sim.TryEnqueue(keepRecords: true, action));
		var preview = sim.World.StateOf(PlayerId);
		Assert.Equal(destination, preview.Position);
		Assert.Equal(60, preview.Heat);
		Assert.Equal(0, preview.CurrentActionPoints);
		Assert.Equal(initialMp, preview.CurrentManeuverPoints);
		Assert.Contains(sim.RecordsFor(0), record => record is Record<MovedFacts>);
		Assert.Contains(sim.RecordsFor(0), record => record is Record<CryoPlumeCooledFacts>);

		battle.Engine.Commit(action);
		var live = battle.Engine.World.StateOf(PlayerId);
		Assert.Equal(preview.Position, live.Position);
		Assert.Equal(preview.Heat, live.Heat);
		Assert.Equal(preview.CurrentActionPoints, live.CurrentActionPoints);
		Assert.Equal(preview.CurrentManeuverPoints, live.CurrentManeuverPoints);
		Assert.Contains(battle.Engine.History(), entry => entry is Record<MovedFacts>);
		Assert.Contains(battle.Engine.History(), entry => entry is Record<CryoPlumeCooledFacts>);

		sim.Dequeue();
		Assert.Equal(origin, preview.Position);
		Assert.Equal(80, preview.Heat);
		Assert.Equal(0, preview.CurrentActionPoints);
		Assert.Equal(initialMp, preview.CurrentManeuverPoints);
		Assert.Empty(preview.CryoPlumesCooledThisTurn);
	}

	private static BattleOrchestrator CreateBattle(
		Unit player,
		Coord? center = null,
		IEnumerable<NonUnit>? extraHazards = null,
		BoundedGrid? grid = null,
		BodyFrame? frame = null)
	{
		grid ??= BattleTestFixture.Grid(16);
		var wellCenter = center ?? Center;
		var well = GravityWell.Create(
			"gravitywell-test", wellCenter, frame ?? BodyFrame.WorldAligned(wellCenter), grid);
		var hazards = new List<NonUnit> { well };
		hazards.AddRange(extraHazards ?? []);
		return BattleTestFixture.BeginSimulation(
			player,
			[BattleTestFixture.Enemy(new Coord(2, 2, 2))],
			grid,
			BattleWorld.TerrainBlockedCells(hazards),
			hazards: hazards);
	}
}
