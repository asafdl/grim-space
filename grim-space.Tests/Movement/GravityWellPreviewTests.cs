using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

[BattleTestSuite]
public sealed class GravityWellPreviewTests
{
	private const string PlayerId = "player";
	private static readonly Coord Center = new(6, 6, 6);
	private static readonly Coord Right = new(1, 0, 0);

	[Fact]
	public void MixedRouteExtendsReachAndQueuedPreviewMatchesCommit()
	{
		var origin = Center + Right - Coord.Forward * 2;
		var entry = origin + Coord.Forward;
		var destination = Center + Right + Coord.Forward * 2;
		using var battle = CreateBattle(origin, 1, Center);
		using var baseline = CreateBattle(origin, 1);
		var sim = battle.Engine.CreateSimulation();
		var cache = new MovePreviewCache();
		var routes = cache.GetPaths(sim, PlayerId);
		var path = routes.Select(route => route.Session).OfType<MovePathSession>().First(route =>
			route.EndPosition == destination && route.EndBasis.Forward == Coord.Forward);
		Assert.Equal(1, path.ExtensionApCost);
		Assert.Equal(4, path.Steps.Count);
		Assert.DoesNotContain(
			MovePathEndpoints.DiscoverExtensions(baseline.Engine.CreateSimulation(), PlayerId),
			route => route.EndPosition == destination);

		var first = routes.Select(route => route.Session).OfType<MovePathSession>().First(route =>
			route.EndPosition == entry && route.EndBasis.Forward == Coord.Forward);
		Assert.Equal(1, first.ExtensionApCost);
		Assert.True(sim.TryEnqueue(first.Steps.ToArray()));
		var extension = cache.GetPaths(sim, PlayerId).Select(route => route.Session).OfType<MovePathSession>().First(route =>
			route.EndPosition == destination && route.EndBasis.Forward == Coord.Forward);
		Assert.Equal(0, extension.ExtensionApCost);
		Assert.Equal(3, extension.Steps.Count);
		Assert.True(sim.TryEnqueue(extension.Steps.ToArray()));

		var preview = sim.World.StateOf(PlayerId);
		Assert.Equal(path.ResultState.Heat, preview.Heat);
		Assert.Equal(path.RemainingAp, preview.CurrentActionPoints);
		battle.Engine.Commit(sim.Actions.ToArray());
		var live = battle.Engine.World.StateOf(PlayerId);
		Assert.Equal(destination, live.Position);
		Assert.Equal(preview.CurrentActionPoints, live.CurrentActionPoints);
		Assert.Equal(preview.Heat, live.Heat);
	}

	[Fact]
	public void ReenteringFromOutsideCostsNormallyAndCanBeFollowedByFreeExit()
	{
		var origin = Center + Right + Coord.Forward * 2;
		var entry = origin - Coord.Forward;
		using var battle = CreateBattle(origin, 2, Center);
		var sim = battle.Engine.CreateSimulation();
		var cache = new MovePreviewCache();
		var reentry = cache.GetPaths(sim, PlayerId).Select(route => route.Session).OfType<MovePathSession>()
			.First(route => route.EndPosition == entry);
		Assert.Equal(2, reentry.ExtensionApCost);
		Assert.True(sim.TryEnqueue(reentry.Steps.ToArray()));

		var exit = cache.GetPaths(sim, PlayerId).Select(route => route.Session).OfType<MovePathSession>()
			.First(route => route.EndPosition == origin);
		Assert.Equal(0, exit.ExtensionApCost);
		Assert.True(sim.TryEnqueue(exit.Steps.ToArray()));
		Assert.Equal(0, sim.World.StateOf(PlayerId).CurrentActionPoints);
	}

	[Fact]
	public void ZeroCostCycleBetweenAdjacentWellsIsPruned()
	{
		var firstCenter = new Coord(4, 6, 6);
		var secondCenter = new Coord(7, 6, 6);
		var origin = firstCenter + Right + Coord.Forward;
		var neighbor = origin + Right;
		using var battle = CreateBattle(origin, 0, firstCenter, secondCenter);
		var sim = battle.Engine.CreateSimulation();
		Assert.True(sim.TryEnqueue(
			new MoveStepAction(PlayerId, ESpatialOrientation.Starboard),
			new MoveStepAction(PlayerId, ESpatialOrientation.Port)));
		Assert.Equal(origin, sim.World.StateOf(PlayerId).Position);
		Assert.Equal(0, sim.World.StateOf(PlayerId).CurrentActionPoints);
		sim.Dequeue(0);

		var paths = MovePathEndpoints.DiscoverExtensions(sim, PlayerId);
		var basis = GridBasis.From(Coord.Forward, Coord.Up, Right);
		var toNeighbor = Assert.Single(paths, path =>
			path.EndPosition == neighbor && path.EndBasis == basis);
		Assert.Equal(0, toNeighbor.ExtensionApCost);
		Assert.Single(toNeighbor.Steps);
		Assert.DoesNotContain(paths, path => path.EndPosition == origin && path.EndBasis == basis);
	}

	[Fact]
	public void SingleWellZeroApReachabilityFitsWithinSearchDepth()
	{
		foreach (var offset in Coord.OffsetsInCube(1).Where(offset => offset != Coord.Zero))
			AssertZeroCostReachable(Center + offset);
	}

	private static void AssertZeroCostReachable(Coord origin)
	{
		using var battle = CreateBattle(origin, 0, Center);
		var world = battle.Engine.World;
		var pending = new Queue<(Coord Cell, int Depth)>();
		var distances = new Dictionary<Coord, int> { [origin] = 0 };
		var headings = new[] { Right, -Right, Coord.Up, -Coord.Up, Coord.Forward, -Coord.Forward };
		pending.Enqueue((origin, 0));
		while (pending.TryDequeue(out var current))
		{
			foreach (var heading in headings)
			{
				var next = current.Cell + heading;
				if (!world.IsTranslationAssisted(current.Cell, heading)
					|| !world.Grid.IsInBounds(next)
					|| world.IsCellBlocked(next)
					|| !distances.TryAdd(next, current.Depth + 1))
					continue;
				pending.Enqueue((next, current.Depth + 1));
			}
		}

		var paths = MovePathEndpoints.DiscoverExtensions(battle.Engine.CreateSimulation(), PlayerId);
		var basis = GridBasis.From(Coord.Forward, Coord.Up, Right);
		var zeroCostPaths = paths.Where(path => path.ExtensionApCost == 0 && path.EndBasis == basis)
			.ToDictionary(path => path.EndPosition);
		Assert.True(distances.Count > 1);
		Assert.True(distances.Values.Max() < 20);
		foreach (var (cell, distance) in distances.Where(entry => entry.Key != origin))
		{
			Assert.True(zeroCostPaths.TryGetValue(cell, out var path), $"Missing zero-cost destination {cell}.");
			Assert.Equal(distance, path!.Steps.Count);
		}
	}

	private static BattleOrchestrator CreateBattle(Coord origin, int ap, params Coord[] centers)
	{
		var grid = BattleTestFixture.Grid(16);
		var player = BattleTestFixture.Player(origin, ap);
		player.State.CurrentManeuverPoints = 0;
		var wells = centers.Select((center, index) =>
			GravityWell.Create($"gravitywell-{index}", center, BodyFrame.WorldAligned(center), grid))
			.ToArray();
		return BattleTestFixture.BeginSimulation(
			player,
			[BattleTestFixture.Enemy(new Coord(1, 1, 1))],
			grid,
			BattleWorld.TerrainBlockedCells(wells),
			hazards: wells);
	}
}
