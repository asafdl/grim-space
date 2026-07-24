using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class MovePathSearchTests
{
	private const string PlayerId = "player";

	[Fact]
	public void OneAndTwoApForwardMovesAreNotReachableEndpoints()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 0);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var options = GetMoveOptions(player, enemy, blocked, origin);
		var endpoints = options.ToDictionary(option => option.EndPosition);

		var oneForward = origin + Coord.Forward;
		var twoForward = origin + Coord.Forward * 2;
		var threeForward = origin + Coord.Forward * 3;
		var threeStepCost = MovementExpectations.TotalApForPureForwardPath(0, 3);

		Assert.False(endpoints.ContainsKey(oneForward));
		Assert.False(endpoints.ContainsKey(twoForward));
		Assert.True(MovementExpectations.IsValidMoveEndpoint(threeStepCost));
		Assert.True(endpoints.ContainsKey(threeForward));
		Assert.Equal(threeStepCost, endpoints[threeForward].ApCost);
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

		var options = GetMoveOptions(player, enemy, blocked, origin);
		var endpoint = origin + Coord.Forward * stepCount;
		var expectedCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		Assert.Equal(0, expectedCost);
		Assert.True(MovementExpectations.IsValidMoveEndpoint(expectedCost));
		Assert.Contains(options, option => option.EndPosition == endpoint && option.ApCost == expectedCost);
	}

	[Fact]
	public void PathCannotContainOpposingDirections()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var zigzag = BattleTestFixture.Path(
			origin,
			0,
			Coord.Forward,
			Coord.Zero - player.State.Fore);

		var frame = BodyFrame.From(player.State);
		var steps = MoveDef.StepsFromPath(PlayerId, frame, origin, zigzag.Path);
		var board = BattleBoard.FromSnapshot(
			[player, BattleTestFixture.Enemy(new Coord(0, 0, 0))],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		var runtime = new ActorSession();
		for (var i = 0; i < steps.Count - 1; i++)
		{
			foreach (var effect in steps[i].Definition.Resolve(steps[i], board, runtime))
				effect.Apply(board, runtime, steps[i].ActorId);
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

		var options = GetMoveOptions(player, enemy, blocked, origin);

		Assert.DoesNotContain(options, option => option.EndPosition.X < 0);
		Assert.DoesNotContain(options, option => option.Path.Contains(origin + Coord.Forward * 2));
	}

	[Fact]
	public void ResolvingForwardMoveRaisesMomentum()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 1;
		var stepCount = 3;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginPlanning(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });

		var option = MovementExpectations.PureForwardMove(origin, stepCount, startMomentum);
		Assert.True(battle.TryEnqueueMovePath(option));

		Assert.Equal(origin + Coord.Forward * stepCount, battle.Board.StateOf(PlayerId).Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(startMomentum, stepCount),
			battle.Board.StateOf(PlayerId).MomentumLevel);
	}

	[Fact]
	public void ResolvingRetroMoveLowersMomentum()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 2);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var retro = BattleTestFixture.Path(origin, 0, Coord.Zero - player.State.Fore);
		var battle = BattleTestFixture.BeginPlanning(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });

		Assert.True(battle.TryEnqueueMovePath(retro));

		Assert.Equal(1, battle.Board.StateOf(PlayerId).MomentumLevel);
	}

	[Fact]
	public void SearchDoesNotMutateSession()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginPlanning(origin);
		var session = battle.Session;
		var actionsBefore = session.Actions.ToList();
		var positionBefore = session.PreviewWorld.StateOf(PlayerId).Position;

		foreach (var _ in session.SearchMoves(PlayerId)) { }

		Assert.Equal(actionsBefore, session.Actions);
		Assert.Equal(positionBefore, session.PreviewWorld.StateOf(PlayerId).Position);
	}

	[Fact]
	public void EndpointOptionsAlignWithTryEnqueueMovePath()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginPlanning(origin);
		var options = GetMoveOptionsFromSession(battle.Session);

		foreach (var option in options)
		{
			var trial = battle.Engine.CreateSimulation();
			BattleOrchestrator.ApplyEndOfPhase(
				trial.PreviewWorld,
				trial.PreviewActorRuntimes.For(PlayerId),
				PlayerId);
			Assert.True(BattleOrchestrator.TryEnqueueMovePath(trial, PlayerId, option));
		}
	}

	private static IReadOnlyList<Option> GetMoveOptions(
		GrimSpace.Battle.Units.Unit player,
		GrimSpace.Battle.Units.Unit enemy,
		IReadOnlySet<Coord> blocked,
		Coord origin)
	{
		var timeline = new Timeline();
		var world = BattleBoard.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			blocked,
			timeline);
		var actorRuntimes = new ActorRuntimes<ActorSession>();
		actorRuntimes.For(PlayerId);
		var session = new Simulation<BattleBoard, ActorSession>(world, actorRuntimes);
		session.Begin(0, 0);
		return GetMoveOptionsFromSession(session, origin, BodyFrame.From(player.State));
	}

	private static IReadOnlyList<Option> GetMoveOptionsFromSession(
		Simulation<BattleBoard, ActorSession> session,
		Coord? origin = null,
		BodyFrame? frame = null)
	{
		origin ??= session.PreviewWorld.StateOf(PlayerId).Position;
		frame ??= BodyFrame.From(session.PreviewWorld.StateOf(PlayerId));
		var startCount = session.Actions.Count;
		var results = new Dictionary<Coord, Option>();

		foreach (var searchFrame in session.SearchMoves(PlayerId))
		{
			var steps = searchFrame.Actions
				.Skip(startCount)
				.OfType<MoveStepAction>()
				.Where(step => step.ActorId == PlayerId)
				.ToList();

			if (steps.Count == 0)
				continue;

			var runtime = searchFrame.Runtimes.For(PlayerId);
			var option = MovePathRules.ToEndpointOption(origin.Value, frame.Value, steps, runtime);
			if (option is null)
				continue;

			if (!results.TryGetValue(option.EndPosition, out var existing) || option.ApCost < existing.ApCost)
				results[option.EndPosition] = option;
		}

		return results.Values.ToList();
	}
}
