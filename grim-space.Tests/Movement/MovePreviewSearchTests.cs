using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class MovePreviewSearchTests
{
	private const string PlayerId = "player";

	[Fact]
	public void PreviewIsIndependentOfLegacyMomentum()
	{
		var origin = new Coord(8, 8, 8);
		var withoutMomentum = Discover(origin, momentum: 0);
		var withMomentum = Discover(origin, momentum: 2);

		Assert.Equal(
			withoutMomentum.Select(Key).OrderBy(key => key),
			withMomentum.Select(Key).OrderBy(key => key));
	}

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
	public void CachedTreeServesMovementBranchesAndUndoWithoutAnotherSearch()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var initial = cache.GetPaths(sim, PlayerId, sim.Actions);
		var move = initial.First(path => path.Steps.Count == 2);

		Assert.Equal(1, cache.BuildCount);
		Assert.True(sim.TryEnqueue(move.Steps.Cast<IAction>().ToArray()));

		var extensions = cache.GetPaths(sim, PlayerId, sim.Actions);
		Assert.Equal(1, cache.BuildCount);
		Assert.NotEmpty(extensions);

		sim.Dequeue(0);
		var afterUndo = cache.GetPaths(sim, PlayerId, sim.Actions);

		Assert.Equal(1, cache.BuildCount);
		Assert.Same(initial, afterUndo);
	}

	[Fact]
	public void CachedBranchMatchesFreshSearch()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var move = cache.GetPaths(sim, PlayerId, sim.Actions)
			.First(path => path.Steps.Count == 1);
		Assert.True(sim.TryEnqueue(move.Steps.Cast<IAction>().ToArray()));

		var cached = cache.GetPaths(sim, PlayerId, sim.Actions);
		var fresh = MovePathEndpoints.DiscoverExtensions(sim, PlayerId);

		Assert.Equal(fresh.Select(PathKey), cached.Select(PathKey));
	}

	[Fact]
	public void QueuedMovementActionsLocateTheSameTreeBranchAcrossWeapons()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var firstMove = cache.GetPaths(sim, PlayerId, sim.Actions)
			.First(path => path.Steps.Count == 1);
		Assert.True(sim.TryEnqueue(firstMove.Steps.Cast<IAction>().ToArray()));
		var afterMove = cache.GetPaths(sim, PlayerId, sim.Actions);

		Assert.True(sim.TryEnqueue(new GrimSpace.Battle.Actions.RailgunAction(PlayerId)));
		var afterWeapon = cache.GetPaths(sim, PlayerId, sim.Actions);

		Assert.Same(afterMove, afterWeapon);
		Assert.Equal(1, cache.BuildCount);
	}

	[Fact]
	public void EveryCachedBranchExtensionReplaysFromItsMovementPrefix()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		var branch = cache.GetPaths(sim, PlayerId, sim.Actions)
			.First(path => path.Steps.Count == 2);
		Assert.True(sim.TryEnqueue(branch.Steps.Cast<IAction>().ToArray()));

		foreach (var extension in cache.GetPaths(sim, PlayerId, sim.Actions))
		{
			var trial = sim.Fork();
			Assert.True(
				trial.TryEnqueue(extension.Steps.Cast<IAction>().ToArray()),
				$"Cached extension to {extension.EndPosition} was not replayable.");
		}
	}

	[Fact]
	public void NonMovementActionsDoNotRebuildMovementTree()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var sim = battle.PlayerAgent.Sim;
		var cache = new MovePreviewCache();
		_ = cache.GetPaths(sim, PlayerId, sim.Actions);

		Assert.True(sim.TryEnqueue(new GrimSpace.Battle.Actions.RailgunAction(PlayerId)));
		Assert.NotEmpty(cache.GetPaths(sim, PlayerId, sim.Actions));
		Assert.Equal(1, cache.BuildCount);
	}

	private static string Key(MovePathSession path) =>
		$"{path.EndPosition}|{path.EndBasis.Forward}|{path.EndBasis.Up}";

	private static string PathKey(MovePathSession path) =>
		$"{Key(path)}|{string.Join(',', path.Steps)}";

	private static IReadOnlyList<MovePathSession> Discover(Coord origin, int momentum = 0)
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin, momentum: momentum),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			BattleTestFixture.Grid(20));
		return MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId);
	}
}
