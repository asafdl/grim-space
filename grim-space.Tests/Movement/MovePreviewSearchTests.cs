using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class MovePreviewSearchTests
{
	private const string PlayerId = "player";

	[Fact]
	public void DiscoverExtensionsRetainsOneRoutePerEndPose()
	{
		var paths = Discover(new Coord(5, 5, 5));

		Assert.Equal(
			paths.Select(Key).Distinct().Count(),
			paths.Count);
		Assert.Contains(paths.GroupBy(path => path.EndPosition), group => group.Count() > 1);
	}

	[Fact]
	public void DefaultOrderingIsDeterministic()
	{
		var origin = new Coord(5, 5, 5);

		Assert.Equal(
			Discover(origin).Select(path => string.Join('|', path.Steps)),
			Discover(origin).Select(path => string.Join('|', path.Steps)));
	}

	[Fact]
	public void CurrentPositionRequiresANonemptyReturnRoute()
	{
		var origin = new Coord(5, 5, 5);
		var paths = Discover(origin);

		Assert.All(paths.Where(path => path.EndPosition == origin), path => Assert.NotEmpty(path.Steps));
		Assert.Contains(paths, path => path.EndPosition == origin);
	}

	[Fact]
	public void PriorityPhaseContainsOnlyDirectMovementRoutes()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var index = MovePathIndex.Start(battle.PlayerAgent.Sim, PlayerId);

		index.CompletePriority();
		var priorityPaths = index.GetExtensions([]);

		Assert.True(index.IsPriorityComplete);
		Assert.False(index.IsComplete);
		Assert.NotEmpty(priorityPaths);
		Assert.All(priorityPaths, path =>
			Assert.All(path.Steps, action => Assert.IsType<GrimSpace.Battle.Actions.MoveStepAction>(action)));
	}

	[Fact]
	public void RemainingPhaseResumesAndRetainsPriorityRoutes()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var index = MovePathIndex.Start(battle.PlayerAgent.Sim, PlayerId);
		index.CompletePriority();
		var priorityKeys = index.GetExtensions([]).Select(PathKey).ToHashSet();

		index.Complete();
		var allPaths = index.GetExtensions([]);

		Assert.True(index.IsComplete);
		Assert.Subset(allPaths.Select(PathKey).ToHashSet(), priorityKeys);
		Assert.Contains(allPaths, path =>
			path.Steps.Any(action => action is GrimSpace.Battle.Actions.HeadingTurnAction
				or GrimSpace.Battle.Actions.RollAction));
	}

	[Fact]
	public void CacheRestoresPreviousPathsAfterUndo()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var initial = cache.GetPaths(sim, PlayerId);
		var move = initial.First(path => path.Session.ExtensionApCost == 2);

		Assert.Equal(1, cache.BuildCount);
		Assert.True(sim.TryEnqueue(move.Session.Steps.Cast<IAction>().ToArray()));

		var extensions = cache.GetPaths(sim, PlayerId);
		Assert.Equal(2, cache.BuildCount);
		Assert.NotEmpty(extensions);

		sim.Dequeue(0);
		var afterUndo = cache.GetPaths(sim, PlayerId);

		Assert.Equal(2, cache.BuildCount);
		Assert.Same(initial, afterUndo);
		Assert.Equal(initial.Select(route => PathKey(route.Session)), afterUndo.Select(route => PathKey(route.Session)));
	}

	[Fact]
	public void CachedBranchMatchesFreshSearch()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var move = cache.GetPaths(sim, PlayerId)
			.First(path => path.Session.ExtensionApCost == 1);
		Assert.True(sim.TryEnqueue(move.Session.Steps.Cast<IAction>().ToArray()));

		var cached = cache.GetPaths(sim, PlayerId);
		var fresh = MovePathEndpoints.DiscoverExtensions(sim, PlayerId);

		Assert.Equal(fresh.Select(PathKey), cached.Select(route => PathKey(route.Session)));
	}

	[Fact]
	public void CacheRebuildsWhenNonMovementQueueChanges()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var firstMove = cache.GetPaths(sim, PlayerId)
			.First(path => path.Session.ExtensionApCost == 1);
		Assert.True(sim.TryEnqueue(firstMove.Session.Steps.Cast<IAction>().ToArray()));
		var afterMove = cache.GetPaths(sim, PlayerId);

		Assert.True(sim.TryEnqueue(new GrimSpace.Battle.Actions.RailgunAction(PlayerId)));
		var afterWeapon = cache.GetPaths(sim, PlayerId);

		Assert.Equal(
			afterMove.Select(route => PathKey(route.Session)),
			afterWeapon.Select(route => PathKey(route.Session)));
		Assert.Equal(3, cache.BuildCount);
	}

	[Fact]
	public void EveryCachedBranchExtensionReplaysFromItsMovementPrefix()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var branch = cache.GetPaths(sim, PlayerId)
			.First(path => path.Session.ExtensionApCost == 2);
		Assert.True(sim.TryEnqueue(branch.Session.Steps.Cast<IAction>().ToArray()));

		foreach (var extension in cache.GetPaths(sim, PlayerId))
		{
			var trial = sim.Fork();
			Assert.True(
				trial.TryEnqueue(extension.Session.Steps.Cast<IAction>().ToArray()),
				$"Cached extension to {extension.Session.EndPosition} was not replayable.");
		}
	}

	[Fact]
	public void HitOpportunitiesLazyComputeOncePerRoute()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(origin + Coord.Forward * 6),
			BattleTestFixture.Grid(20));
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var route = cache.GetPaths(sim, PlayerId)
			.First(path => path.Session.EndPosition == origin);

		Assert.Equal(0, route.ComputeCount);
		var first = route.GetHitOpportunities(sim, PlayerId);
		Assert.Equal(1, route.ComputeCount);
		var second = route.GetHitOpportunities(sim, PlayerId);

		Assert.Equal(1, route.ComputeCount);
		Assert.Same(first, second);
	}

	[Fact]
	public void HitOpportunitiesReusePriorRouteWithoutRecompute()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(origin + Coord.Forward * 6),
			BattleTestFixture.Grid(20));
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var routes = cache.GetPaths(sim, PlayerId);
		var routeA = routes[0];
		var routeB = routes[1];

		routeA.GetHitOpportunities(sim, PlayerId);
		routeB.GetHitOpportunities(sim, PlayerId);
		Assert.Equal(1, routeA.ComputeCount);
		Assert.Equal(1, routeB.ComputeCount);

		routeA.GetHitOpportunities(sim, PlayerId);

		Assert.Equal(1, routeA.ComputeCount);
	}

	[Fact]
	public void EmptyHitOpportunitiesAreMemoized()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			BattleTestFixture.Grid(20));
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var route = cache.GetPaths(sim, PlayerId)
			.First(path => path.Session.EndPosition == origin);

		var first = route.GetHitOpportunities(sim, PlayerId);
		var second = route.GetHitOpportunities(sim, PlayerId);

		Assert.Empty(first);
		Assert.Same(first, second);
		Assert.Equal(1, route.ComputeCount);
	}

	[Fact]
	public void RepeatedRequestForSameStateUsesCachedPaths()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var first = cache.GetPaths(sim, PlayerId);

		var second = cache.GetPaths(sim, PlayerId);

		Assert.Same(first, second);
		Assert.Equal(1, cache.BuildCount);
	}

	private static string Key(MovePathSession path) =>
		$"{path.EndPosition}|{path.EndBasis.Forward}|{path.EndBasis.Up}";

	private static string PathKey(MovePathSession path) =>
		$"{Key(path)}|{string.Join(',', path.Steps)}";

	private static IReadOnlyList<MovePathSession> Discover(Coord origin)
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			BattleTestFixture.Grid(20));
		return MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId);
	}
}
