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

	[Theory]
	[InlineData(28f, 1)]
	[InlineData(28.01f, 0)]
	public void FiringSnapUsesEighteenPixelAcquisitionBand(float firingDistance, int expectedX)
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 10f, 40f, 10f),
			Candidate(1, 0, 0, firingDistance, 40f, 20f),
		};
		Assert.Equal(new Coord(expectedX, 0, 0), MovementPick.Resolve(candidates,
			preference: cell => new MovementPickPreference(cell.X == 1, 0, 0)));
	}

	[Fact]
	public void DirectAimWinsOverFiringPreferenceAndRetainedHover()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 0.5f, 40f, 20f),
			Candidate(1, 0, 0, 1f, 40f, 10f),
		};
		Assert.Equal(Coord.Zero, MovementPick.Resolve(candidates, new Coord(1, 0, 0),
			cell => new MovementPickPreference(cell.X == 1, 0, 0)));
	}

	[Fact]
	public void AssistCannotRetainHoverOutsideAmbiguityBand()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 10f, 40f, 20f),
			Candidate(1, 0, 0, 34.01f, 40f, 10f),
		};
		Assert.Equal(Coord.Zero, MovementPick.Resolve(candidates, new Coord(1, 0, 0),
			_ => new MovementPickPreference(true, 2, 0)));
	}

	[Fact]
	public void FiringOpportunityOutranksGeometricAlignmentWithinTie()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 10f, 40f, 10f),
			Candidate(1, 0, 0, 12f, 40f, 20f),
		};
		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates, Coord.Zero,
			cell => cell.X == 0
				? new MovementPickPreference(false, 2, 0)
				: new MovementPickPreference(true, 0, 4)));
	}

	[Fact]
	public void EqualTacticalPreferencesPreserveStableBaselineTieBreaking()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 10f, 40f, 20f),
			Candidate(1, 0, 0, 11f, 40f, 10f),
		};
		MovementPickPreference Preference(Coord _) => new(false, 1, 2);
		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates, preference: Preference));
		Assert.Equal(Coord.Zero, MovementPick.Resolve(candidates, Coord.Zero, Preference));
		Assert.Equal(MovementPick.Resolve(candidates, preference: Preference),
			MovementPick.Resolve(candidates.Reverse().ToArray(), preference: Preference));
	}

	[Theory]
	[InlineData(0, 2, 3)]
	[InlineData(2, 0, 3)]
	[InlineData(2, 3, 0)]
	public void AlignmentTreatsAllAxesEqually(int x, int y, int z)
	{
		Assert.Equal(new MovementPickPreference(false, 1, 2),
			MovementPick.Alignment(new Coord(x, y, z), Coord.Zero, false));
	}

	[Theory]
	[InlineData(5, 0, 0)]
	[InlineData(-5, 0, 0)]
	[InlineData(0, 5, 0)]
	[InlineData(0, -5, 0)]
	[InlineData(0, 0, 5)]
	[InlineData(0, 0, -5)]
	public void AssistPrefersFiringAxisLineOverOffAxisFiringCell(int x, int y, int z)
	{
		var enemy = new Coord(10, 10, 10);
		var aligned = enemy + new Coord(x, y, z);
		var offAxis = enemy + new Coord(1, 2, 3);
		var candidates = new[]
		{
			new MovementPickCandidate(offAxis, 10f, 40f, 10f),
			new MovementPickCandidate(aligned, 15f, 40f, 30f),
		};
		Assert.Equal(aligned, MovementPick.Resolve(candidates, offAxis,
			cell => MovementPick.Alignment(cell, enemy, canHit: true)));
	}

	[Fact]
	public void AlignmentProgressReducesDistanceToNearestAxisLine()
	{
		var before = MovementPick.Alignment(new Coord(3, 4, 5), Coord.Zero, false);
		var after = MovementPick.Alignment(new Coord(2, 4, 5), Coord.Zero, false);
		Assert.Equal(0, before.AlignedAxes);
		Assert.Equal(0, after.AlignedAxes);
		Assert.True(after.AlignmentError < before.AlignmentError);
	}

	[Fact]
	public void AssistPrefersAlignedPlaneThenProgressWithoutFavoringCameraDepth()
	{
		var candidates = new[]
		{
			Candidate(2, 3, 4, 10f, 40f, 10f),
			Candidate(0, 3, 4, 12f, 40f, 20f),
			Candidate(0, 2, 4, 13f, 40f, 30f),
		};
		Assert.Equal(new Coord(0, 2, 4), MovementPick.Resolve(candidates,
			preference: cell => MovementPick.Alignment(cell, Coord.Zero, false)));
	}

	[Fact]
	public void AssistOnlyEvaluatesPreferencesInsideCursorBand()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 10f, 40f, 10f),
			Candidate(1, 0, 0, 12f, 40f, 20f),
			Candidate(2, 0, 0, 29f, 40f, 30f),
		};
		var scored = new List<Coord>();
		MovementPick.Resolve(candidates, preference: cell =>
		{
			scored.Add(cell);
			return new MovementPickPreference(false, 0, 0);
		});
		Assert.Equal(new[] { Coord.Zero, new Coord(1, 0, 0) }, scored);
	}

	[Fact]
	public void AssistDoesNotScoreIneligibleCandidates()
	{
		var invalid = Candidate(0, 0, 0, 50f, 24f, 10f);
		Assert.Null(MovementPick.Resolve([invalid],
			preference: _ => throw new InvalidOperationException("Ineligible candidate scored.")));
	}

	[Theory]
	[InlineData(34f, 1)]
	[InlineData(34.01f, 0)]
	public void StrongSnapReleasesBeyondTwentyFourPixelBand(float retainedDistance, int expectedX)
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 10f, 40f, 10f),
			Candidate(1, 0, 0, retainedDistance, 40f, 20f),
		};
		Assert.Equal(new Coord(expectedX, 0, 0), MovementPick.Resolve(candidates, new Coord(1, 0, 0),
			cell => new MovementPickPreference(cell.X == 1, 0, 0)));
	}

	[Theory]
	[InlineData(16f, 1)]
	[InlineData(16.01f, 0)]
	public void OrdinaryAlignmentStillUsesWeakSixPixelBand(float distance, int expectedX)
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 10f, 40f, 10f),
			Candidate(1, 0, 0, distance, 40f, 20f),
		};
		Assert.Equal(new Coord(expectedX, 0, 0), MovementPick.Resolve(candidates,
			preference: cell => new MovementPickPreference(false, cell.X, 0)));
	}

	[Fact]
	public void ExactEnemyAxisLineGetsStrongSnapWithoutConfirmedShot()
	{
		var candidates = new[]
		{
			Candidate(0, 0, 0, 10f, 40f, 10f),
			Candidate(1, 0, 0, 28f, 40f, 20f),
		};
		Assert.Equal(new Coord(1, 0, 0), MovementPick.Resolve(candidates,
			preference: cell => new MovementPickPreference(false, cell.X * 2, 0)));
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
