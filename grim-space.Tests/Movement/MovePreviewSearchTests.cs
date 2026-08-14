using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class MovePreviewSearchTests
{
	private const string PlayerId = "player";

	[Theory]
	[MemberData(nameof(MovementExpectations.ReachablePreviewByMomentum), MemberType = typeof(MovementExpectations))]
	public void PreviewReachableCellsMatchMomentumTable(
		int momentum,
		int expectedVisible,
		int expectedCanEnd,
		int expectedFurthestForward)
	{
		var origin = new Coord(8, 8, 8);
		var paths = DiscoverReachable(origin, momentum);

		Assert.Equal(expectedVisible, paths.Count);
		Assert.Equal(paths.Select(path => path.EndPosition).Distinct().Count(), paths.Count);
		Assert.Equal(expectedCanEnd, paths.Count(path => path.CanEndPath));

		var forwardAxis = BodyFrame.WorldAligned(origin).Fore;
		var furthestForward = paths
			.Where(path => path.Steps.All(step => step.Direction == ESpatialOrientation.Forward))
			.Select(path => path.EndPosition.ManhattanDistanceTo(origin))
			.DefaultIfEmpty(0)
			.Max();
		Assert.Equal(expectedFurthestForward, furthestForward);
		Assert.Contains(
			paths,
			path => path.EndPosition == origin + forwardAxis * expectedFurthestForward);
	}

	[Fact]
	public void DiscoverExtensionsRetainsExactlyOneResultPerCell()
	{
		var origin = new Coord(5, 5, 5);
		var paths = Discover(origin);

		Assert.Equal(
			paths.Select(path => path.EndPosition).Distinct().Count(),
			paths.Count);
	}

	[Fact]
	public void IntermediateCellsAreVisibleAndEndable()
	{
		var origin = new Coord(5, 5, 5);
		var paths = Discover(origin).ToDictionary(path => path.EndPosition);

		Assert.True(paths.ContainsKey(origin + Coord.Forward));
		Assert.True(paths[origin + Coord.Forward].CanEndPath);
		Assert.True(paths.ContainsKey(origin + Coord.Forward * 3));
		Assert.True(paths[origin + Coord.Forward * 3].CanEndPath);
	}

	[Fact]
	public void EveryProjectedPathIsReplayableViaTryEnqueue()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var paths = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId);

		Assert.NotEmpty(paths);
		foreach (var path in paths)
		{
			var trial = battle.PlayerAgent.Sim.Fork();
			Assert.True(BattleTestActions.TryEnqueueMovePath(trial, PlayerId, path));
		}
	}

	/// <summary>
	/// Spacious fixture so free-forward reach (up to 6 cells) is not clipped by the grid edge.
	/// </summary>
	private static IReadOnlyList<MovePathSession> DiscoverReachable(Coord origin, int momentum)
	{
		var player = BattleTestFixture.Player(origin, momentum: momentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var world = BattleWorld.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(20),
			new HashSet<Coord> { enemy.State.Position });
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(PlayerId);
		var session = new Simulation<BattleWorld, ActorRuntime>(world, actorRuntimes);
		session.Begin(0, 0);
		return MovePathEndpoints.DiscoverExtensions(session, PlayerId);
	}

	private static IReadOnlyList<MovePathSession> Discover(Coord origin, int momentum = 0)
	{
		var player = BattleTestFixture.Player(origin, momentum: momentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var world = BattleWorld.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(PlayerId);
		var session = new Simulation<BattleWorld, ActorRuntime>(world, actorRuntimes);
		session.Begin(0, 0);
		return MovePathEndpoints.DiscoverExtensions(session, PlayerId);
	}
}
