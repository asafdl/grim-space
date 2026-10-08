using System.Collections.Frozen;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Spatial;

[BattleTestSuite]
[Collection(AbilityAreaCacheCollection.Name)]
public sealed class AbilityAreaCacheTests
{
	public AbilityAreaCacheTests() => AbilityArea.ClearCachesForTesting();

	private static FrozenSet<Coord> Frozen(params Coord[] cells) =>
		cells.ToFrozenSet();

	[Fact]
	public void NonBlockableLeavesCachedGeometryUntouchedAcrossCalls()
	{
		var origin = new Coord(5, 5, 5);
		var candidates = Frozen(new Coord(6, 5, 5), new Coord(7, 5, 5));
		var first = AbilityArea.ApplyBlocking(origin, candidates, false, Frozen(new Coord(6, 5, 5)));
		var second = AbilityArea.ApplyBlocking(origin, candidates, false, Frozen(new Coord(6, 5, 5)));

		Assert.Same(candidates, first);
		Assert.Same(first, second);
	}

	[Fact]
	public void BlockedResultIsReusedForStableGeometryAndTopology()
	{
		var origin = Coord.Zero;
		var candidates = Frozen(new Coord(1, 0, 0), new Coord(3, 0, 0));
		var blockers = Frozen(new Coord(2, 0, 0));

		var first = AbilityArea.ApplyBlocking(origin, candidates, true, blockers);
		var second = AbilityArea.ApplyBlocking(origin, candidates, true, blockers);

		Assert.Same(first, second);
	}

	[Fact]
	public void AffectedCellsReuseBlockedResultAcrossWorldForksAndInvalidateOnTopologyChange()
	{
		const string playerId = "player";
		var playerPos = new Coord(5, 5, 5);
		var asteroidPos = playerPos + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			playerPos,
			playerPos + Coord.Forward * 6);
		var world = battle.PlayerAgent.Sim.World;
		BattleTestWorld.InjectNonUnit(
			world,
			Asteroid.Create("asteroid", asteroidPos, world.Grid, [asteroidPos]));
		var action = new LightningCannonAction(playerId);

		var first = LightningCannonDef.Instance.AffectedCells(action, world);
		var repeated = LightningCannonDef.Instance.AffectedCells(action, world);
		var forked = LightningCannonDef.Instance.AffectedCells(action, world.Fork());

		Assert.Same(first, repeated);
		Assert.Same(first, forked);

		Assert.True(world.RemoveNonUnit("asteroid"));
		var afterRemoval = LightningCannonDef.Instance.AffectedCells(action, world);
		Assert.NotSame(first, afterRemoval);
		Assert.Contains(asteroidPos, afterRemoval);
	}
}
