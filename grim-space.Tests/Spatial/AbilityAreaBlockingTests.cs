using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Spatial;

[BattleTestSuite]
public sealed class AbilityAreaBlockingTests
{
	private static readonly GrimSpace.Math.Grid.Grid TestGrid = BattleTestFixture.Grid();

	private static Hazard AsteroidAt(Coord cell) =>
		Hazard.Asteroid($"rock-{cell}", cell, TestGrid, [cell]);

	[Fact]
	public void AxialBlockerExcludesItselfAndCellsBehind()
	{
		var origin = new Coord(0, 0, 0);
		var blocker = new Coord(3, 0, 0);
		var behind = new Coord(5, 0, 0);
		var candidates = new HashSet<Coord> { blocker, behind, new Coord(1, 0, 0) };
		var filtered = AbilityArea.ApplyBlocking(
			origin,
			candidates,
			isBlockable: true,
			[AsteroidAt(blocker)]);

		Assert.Contains(new Coord(1, 0, 0), filtered);
		Assert.DoesNotContain(blocker, filtered);
		Assert.DoesNotContain(behind, filtered);
	}

	[Fact]
	public void CellsBeforeBlockerRemain()
	{
		var origin = new Coord(0, 0, 0);
		var before = new Coord(2, 0, 0);
		var blocker = new Coord(4, 0, 0);
		var candidates = new HashSet<Coord> { before, blocker, new Coord(6, 0, 0) };
		var filtered = AbilityArea.ApplyBlocking(
			origin,
			candidates,
			isBlockable: true,
			[AsteroidAt(blocker)]);

		Assert.Contains(before, filtered);
		Assert.DoesNotContain(blocker, filtered);
		Assert.DoesNotContain(new Coord(6, 0, 0), filtered);
	}

	[Fact]
	public void OffAxisBlockerHasNoEffect()
	{
		var origin = new Coord(0, 0, 0);
		var target = new Coord(5, 0, 0);
		var candidates = new HashSet<Coord> { target };
		var filtered = AbilityArea.ApplyBlocking(
			origin,
			candidates,
			isBlockable: true,
			[AsteroidAt(new Coord(3, 1, 0))]);

		Assert.Contains(target, filtered);
	}

	[Fact]
	public void OriginNeverSelfBlocks()
	{
		var origin = new Coord(2, 2, 2);
		var candidates = new HashSet<Coord> { origin, new Coord(4, 2, 2) };
		var filtered = AbilityArea.ApplyBlocking(
			origin,
			candidates,
			isBlockable: true,
			[AsteroidAt(origin)]);

		Assert.Contains(origin, filtered);
	}

	[Fact]
	public void DiagonalEdgeTieIsBlocked()
	{
		var origin = new Coord(0, 0, 0);
		var corner = new Coord(1, 1, 0);
		var candidates = new HashSet<Coord> { new Coord(2, 2, 0) };
		var filtered = AbilityArea.ApplyBlocking(
			origin,
			candidates,
			isBlockable: true,
			[AsteroidAt(corner)]);

		Assert.DoesNotContain(new Coord(2, 2, 0), filtered);
	}

	[Fact]
	public void ThreeAxisCornerTieIsBlocked()
	{
		var origin = new Coord(0, 0, 0);
		var corner = new Coord(1, 1, 1);
		var candidates = new HashSet<Coord> { new Coord(2, 2, 2) };
		var filtered = AbilityArea.ApplyBlocking(
			origin,
			candidates,
			isBlockable: true,
			[AsteroidAt(corner)]);

		Assert.DoesNotContain(new Coord(2, 2, 2), filtered);
	}

	[Fact]
	public void MultipleBlockersAreDeterministic()
	{
		var origin = new Coord(0, 0, 0);
		var first = new Coord(2, 0, 0);
		var second = new Coord(4, 0, 0);
		var candidates = new HashSet<Coord> { new Coord(1, 0, 0), first, new Coord(3, 0, 0), second, new Coord(6, 0, 0) };
		var hazards = new[] { AsteroidAt(second), AsteroidAt(first) };
		var filtered = AbilityArea.ApplyBlocking(origin, candidates, true, hazards);

		Assert.Contains(new Coord(1, 0, 0), filtered);
		Assert.DoesNotContain(first, filtered);
		Assert.DoesNotContain(new Coord(3, 0, 0), filtered);
		Assert.DoesNotContain(second, filtered);
		Assert.DoesNotContain(new Coord(6, 0, 0), filtered);
	}

	[Fact]
	public void EmptyBlockerSetPreservesAllCandidates()
	{
		var origin = new Coord(1, 1, 1);
		var candidates = new HashSet<Coord> { new Coord(2, 1, 1), new Coord(3, 1, 1) };
		var filtered = AbilityArea.ApplyBlocking(origin, candidates, true, []);

		Assert.Equal(candidates, filtered);
	}

	[Fact]
	public void ResultDoesNotMutateOriginalCandidateSet()
	{
		var origin = new Coord(0, 0, 0);
		var candidates = new HashSet<Coord> { new Coord(5, 0, 0) };
		var snapshot = candidates.ToHashSet();
		_ = AbilityArea.ApplyBlocking(origin, candidates, true, [AsteroidAt(new Coord(3, 0, 0))]);
		Assert.Equal(snapshot, candidates);
	}

	[Fact]
	public void NonBlockableLeavesCachedGeometryUntouchedAcrossCalls()
	{
		var origin = new Coord(5, 5, 5);
		var candidates = new HashSet<Coord> { new Coord(6, 5, 5), new Coord(7, 5, 5) };
		var first = AbilityArea.ApplyBlocking(origin, candidates, false, [AsteroidAt(new Coord(6, 5, 5))]);
		candidates.Add(new Coord(8, 5, 5));
		var second = AbilityArea.ApplyBlocking(origin, candidates, false, [AsteroidAt(new Coord(6, 5, 5))]);

		Assert.Equal(3, second.Count);
		Assert.Contains(new Coord(8, 5, 5), second);
		Assert.Equal(first.Count + 1, second.Count);
	}
}
