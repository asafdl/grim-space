using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class MovementPickingTests
{
	[Fact]
	public void SmallerPixelDistanceWinsOutsideTieBand()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, distancePx: 15f, radiusPx: 40f, depth: 20f),
			Candidate(1, 0, 0, distancePx: 10f, radiusPx: 24f, depth: 50f),
		};

		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates));
	}

	[Fact]
	public void DifferentRadiiDoNotDistortProximityRanking()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, distancePx: 18f, radiusPx: 64f, depth: 5f),
			Candidate(1, 0, 0, distancePx: 12f, radiusPx: 24f, depth: 50f),
		};

		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates));
	}

	[Fact]
	public void TieBandUsesDepthBeforeCoordinateOrder()
	{
		var candidates = new[]
		{
			Candidate(2, 0, 0, distancePx: 10f, radiusPx: 40f, depth: 30f),
			Candidate(1, 0, 0, distancePx: 11f, radiusPx: 40f, depth: 10f),
		};

		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates));
	}

	[Fact]
	public void EqualDepthBreaksTiesByCoordinate()
	{
		var candidates = new[]
		{
			Candidate(2, 1, 0, distancePx: 10f, radiusPx: 40f, depth: 10f),
			Candidate(1, 2, 0, distancePx: 10.5f, radiusPx: 40f, depth: 10f),
			Candidate(1, 1, 1, distancePx: 11f, radiusPx: 40f, depth: 10f),
		};

		Assert.Equal(new Coord(1, 1, 1), MovementPick.Resolve(candidates));
	}

	[Fact]
	public void CandidateOrderDoesNotChangeSelection()
	{
		var first = new[]
		{
			Candidate(0, 0, 0, distancePx: 10f, radiusPx: 40f, depth: 50f),
			Candidate(1, 0, 0, distancePx: 15f, radiusPx: 40f, depth: 10f),
			Candidate(2, 0, 0, distancePx: 20f, radiusPx: 40f, depth: 5f),
		};
		var second = first.Reverse().ToArray();

		Assert.Equal(MovementPick.Resolve(first), MovementPick.Resolve(second));
		Assert.Equal(new Coord(0, 0, 0), MovementPick.Resolve(first));
	}

	[Fact]
	public void ThreeWayDistanceTieAvoidsNonTransitivePairwiseEpsilon()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, distancePx: 10.0f, radiusPx: 40f, depth: 30f),
			Candidate(1, 0, 0, distancePx: 11.9f, radiusPx: 40f, depth: 10f),
			Candidate(2, 0, 0, distancePx: 11.0f, radiusPx: 40f, depth: 20f),
		};

		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates));
	}

	[Theory]
	[InlineData(24f, 24f, true)]
	[InlineData(24.1f, 24f, false)]
	[InlineData(10f, 64f, true)]
	[InlineData(64f, 64f, true)]
	[InlineData(64.1f, 64f, false)]
	public void RadiusEligibilityUsesInclusiveEdge(float distancePx, float radiusPx, bool eligible)
	{
		var candidate = Candidate(0, 0, 0, distancePx, radiusPx, depth: 10f);
		Assert.Equal(eligible, MovementPick.IsEligible(candidate));
	}

	[Fact]
	public void EmptyCandidateSetReturnsNull()
	{
		Assert.Null(MovementPick.Resolve([]));
	}

	[Fact]
	public void NonFiniteInputsAreRejected()
	{
		var candidates = new[]
		{
			new MovementPickCandidate(new Coord(0, 0, 0), float.NaN, 40f, 10f),
			new MovementPickCandidate(new Coord(1, 0, 0), 10f, float.PositiveInfinity, 10f),
			new MovementPickCandidate(new Coord(2, 0, 0), 10f, 40f, float.NegativeInfinity),
		};

		Assert.Null(MovementPick.Resolve(candidates));
	}

	[Fact]
	public void HoverRetainedForMarginalImprovement()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, distancePx: 20f, radiusPx: 40f, depth: 10f),
			Candidate(1, 0, 0, distancePx: 17f, radiusPx: 40f, depth: 5f),
		};

		Assert.Equal(new Coord(0, 0, 0), MovementPick.Resolve(candidates, new Coord(0, 0, 0)));
	}

	[Fact]
	public void HoverSwitchesBeyondHysteresisThreshold()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, distancePx: 20f, radiusPx: 40f, depth: 10f),
			Candidate(1, 0, 0, distancePx: 14f, radiusPx: 40f, depth: 5f),
		};

		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates, new Coord(0, 0, 0)));
	}

	[Fact]
	public void ExactHysteresisThresholdRetainsCurrentHover()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, distancePx: 20f, radiusPx: 40f, depth: 10f),
			Candidate(1, 0, 0, distancePx: 16f, radiusPx: 40f, depth: 5f),
		};

		Assert.Equal(new Coord(0, 0, 0), MovementPick.Resolve(candidates, new Coord(0, 0, 0)));
	}

	[Fact]
	public void ZeroDistanceHoverRemainsStableAgainstEqualDistanceChallenger()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, distancePx: 0f, radiusPx: 40f, depth: 10f),
			Candidate(1, 0, 0, distancePx: 0f, radiusPx: 40f, depth: 5f),
		};

		Assert.Equal(new Coord(0, 0, 0), MovementPick.Resolve(candidates, new Coord(0, 0, 0)));
	}

	[Fact]
	public void RemovedRetainedCoordinateDoesNotReceiveRetentionAdvantage()
	{
		var candidates = new[]
		{
			Candidate(1, 0, 0, distancePx: 12f, radiusPx: 40f, depth: 5f),
		};

		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates, new Coord(0, 0, 0)));
	}

	[Fact]
	public void RetainedCoordinateOutsideRadiusDoesNotReceiveRetentionAdvantage()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, distancePx: 30f, radiusPx: 24f, depth: 10f),
			Candidate(1, 0, 0, distancePx: 12f, radiusPx: 40f, depth: 5f),
		};

		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates, new Coord(0, 0, 0)));
	}

	[Fact]
	public void DuplicateDestinationCandidatesDoNotChangeSpatialSelection()
	{
		var unique = new[]
		{
			Candidate(1, 0, 0, distancePx: 10f, radiusPx: 40f, depth: 20f),
			Candidate(2, 0, 0, distancePx: 15f, radiusPx: 40f, depth: 10f),
		};
		var duplicated = unique
			.Concat(unique.Where(candidate => candidate.Coordinate == new Coord(1, 0, 0)))
			.ToArray();

		Assert.Equal(MovementPick.Resolve(unique), MovementPick.Resolve(duplicated));
	}

	private static MovementPickCandidate Candidate(
		int x,
		int y,
		int z,
		float distancePx,
		float radiusPx,
		float depth) =>
		new(new Coord(x, y, z), distancePx, radiusPx, depth);
}
