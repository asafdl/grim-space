using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Movement;

public sealed class MovePathSearchTests
{
	private const string PlayerId = "player";

	[Fact]
	public void DuplicateEndpointPrefersShortestPathOverLowerApCost()
	{
		var origin = new Coord(0, 0, 0);
		var frame = BodyFrame.WorldAligned(origin);
		var end = new Coord(2, 0, 0);
		var shorter = MovePathSession.Begin(PlayerId, origin, frame, 0, Stats.ForType(EType.Fighter).MinPathApCost);
		shorter.Steps.Add(new MoveStepAction(PlayerId, ESpatialOrientation.Forward));
		shorter.Steps.Add(new MoveStepAction(PlayerId, ESpatialOrientation.Forward));
		shorter.Cells.Add(new Coord(1, 0, 0));
		shorter.Cells.Add(end);
		shorter.PathApSpent = 4;

		var longer = MovePathSession.Begin(PlayerId, origin, frame, 0, Stats.ForType(EType.Fighter).MinPathApCost);
		longer.Steps.Add(new MoveStepAction(PlayerId, ESpatialOrientation.Forward));
		longer.Steps.Add(new MoveStepAction(PlayerId, ESpatialOrientation.Port));
		longer.Steps.Add(new MoveStepAction(PlayerId, ESpatialOrientation.Forward));
		longer.Steps.Add(new MoveStepAction(PlayerId, ESpatialOrientation.Starboard));
		longer.Cells.Add(new Coord(1, 0, 0));
		longer.Cells.Add(new Coord(1, 1, 0));
		longer.Cells.Add(new Coord(2, 1, 0));
		longer.Cells.Add(end);
		longer.PathApSpent = 2;

		Assert.True(MovePathSession.PreferPath(shorter, longer));
		Assert.False(MovePathSession.PreferPath(longer, shorter));
	}

	[Fact]
	public void OneAndTwoApForwardMovesAreVisibleIntermediateCells()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 0);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var options = GetMovePaths(player, enemy, blocked, origin);
		var endpoints = options.ToDictionary(path => path.EndPosition);

		var oneForward = origin + Coord.Forward;
		var twoForward = origin + Coord.Forward * 2;
		var threeForward = origin + Coord.Forward * 3;
		var threeStepCost = MovementExpectations.TotalApForPureForwardPath(0, 3);

		Assert.True(endpoints.ContainsKey(oneForward));
		Assert.False(endpoints[oneForward].CanEndPath);
		Assert.True(endpoints.ContainsKey(twoForward));
		Assert.False(endpoints[twoForward].CanEndPath);
		Assert.True(MovementExpectations.IsValidMoveEndpoint(threeStepCost));
		Assert.True(endpoints.ContainsKey(threeForward));
		Assert.True(endpoints[threeForward].CanEndPath);
		Assert.Equal(threeStepCost, endpoints[threeForward].PathApSpent);
	}

	[Fact]
	public void HighMomentumCanReachForwardEndpointWithoutSpendingAp()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = MovementExpectations.MaxMomentum;
		var stepCount = startMomentum;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { enemy.State.Position };

		var options = GetMovePaths(player, enemy, blocked, origin);
		var endpoint = origin + Coord.Forward * stepCount;
		var expectedCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		Assert.Equal(0, expectedCost);
		Assert.True(MovementExpectations.IsValidMoveEndpoint(expectedCost));
		Assert.Contains(options, path => path.EndPosition == endpoint && path.PathApSpent == expectedCost);
	}

	[Fact]
	public void PathCannotContainOpposingDirections()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var zigzag = BattleTestFixture.Path(
			PlayerId,
			origin,
			0,
			Coord.Forward,
			Coord.Zero - player.State.Fore);

		var board = BattleWorld.FromSnapshot(
			[player, BattleTestFixture.Enemy(new Coord(0, 0, 0))],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		var runtime = new ActorRuntime();
		var steps = zigzag.Steps;
		for (var i = 0; i < steps.Count - 1; i++)
		{
			foreach (var effect in steps[i].Definition.Resolve(steps[i], board, runtime))
				_ = effect.Apply(board, runtime, steps[i].ActorId);
		}

		Assert.False(MoveDef.Instance.IsLegal(steps[^1], board, runtime));
	}

	[Fact]
	public void PathsCannotPassThroughBlockedCellsOrLeaveGrid()
	{
		var origin = new Coord(0, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { origin + Coord.Forward * 2, enemy.State.Position };

		var options = GetMovePaths(player, enemy, blocked, origin);

		Assert.DoesNotContain(options, path => path.EndPosition.X < 0);
		Assert.DoesNotContain(options, path => path.Cells.Contains(origin + Coord.Forward * 2));
	}

	[Fact]
	public void EnqueueForwardMovePathRaisesMomentum()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 1;
		var stepCount = 3;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });

		var path = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount, startMomentum);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, path));

		Assert.Equal(origin + Coord.Forward * stepCount, battle.Sim.StateOf<ActorState>(PlayerId).Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(startMomentum, stepCount),
			battle.Sim.StateOf<ActorState>(PlayerId).MomentumLevel);
	}

	[Fact]
	public void EnqueueRetroMovePathLowersMomentum()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 2);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var retro = BattleTestFixture.Path(PlayerId, origin, 0, Coord.Zero - player.State.Fore);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });

		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, retro));

		Assert.Equal(1, battle.Sim.StateOf<ActorState>(PlayerId).MomentumLevel);
	}

	[Fact]
	public void SearchDoesNotMutateSimulation()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var session = battle.Sim;
		var actionsBefore = session.Actions.ToList();
		var positionBefore = session.StateOf<ActorState>(PlayerId).Position;

		foreach (var _ in ActionSearch.Run(session, PlayerId, [MoveDef.Instance], BattleSearchVisit.ForCapabilities)) { }

		Assert.Equal(actionsBefore, session.Actions);
		Assert.Equal(positionBefore, session.StateOf<ActorState>(PlayerId).Position);
	}

	[Fact]
	public void EndpointOptionsAlignWithTryEnqueueMovePath()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var paths = MovePathEndpoints.DiscoverExtensions(battle.Sim, battle.PlayerId);

		foreach (var path in paths)
		{
			var trial = BattleTestFixture.CreateTrialSimulation(battle);
			Assert.True(BattleTestActions.TryEnqueueMovePath(trial, PlayerId, path));
		}
	}

	private static IReadOnlyList<MovePathSession> GetMovePaths(
		GrimSpace.Battle.Units.Unit player,
		GrimSpace.Battle.Units.Unit enemy,
		IReadOnlySet<Coord> blocked,
		Coord origin)
	{
		var timeline = new Timeline();
		var world = BattleWorld.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			blocked,
			timeline);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(PlayerId);
		var session = new Simulation<BattleWorld, ActorRuntime>(world, actorRuntimes);
		session.Begin(0, 0);
		return MovePathEndpoints.DiscoverExtensions(session, PlayerId);
	}
}
