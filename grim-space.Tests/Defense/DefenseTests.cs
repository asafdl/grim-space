using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.ShieldDefense;

public sealed class HitFaceTests
{
	private static BodyFrame FrameAt(Coord position) =>
		BodyFrame.From(MakeState(position));

	private static State MakeState(Coord position) =>
		State.FromSpawn(new Instance { Id = "test", Type = EType.Fighter }, position);

	[Theory]
	[InlineData(1, 0, 0, ESpatialOrientation.Starboard)]
	[InlineData(-1, 0, 0, ESpatialOrientation.Port)]
	[InlineData(0, 1, 0, ESpatialOrientation.Dorsal)]
	[InlineData(0, -1, 0, ESpatialOrientation.Ventral)]
	[InlineData(0, 0, 1, ESpatialOrientation.Forward)]
	[InlineData(0, 0, -1, ESpatialOrientation.Retro)]
	public void HitFaceFrom_WorldAxisAttack_SelectsExpectedFace(int dx, int dy, int dz, ESpatialOrientation expected)
	{
		var origin = new Coord(5, 5, 5);
		var frame = FrameAt(origin);
		var attackOrigin = origin + new Coord(dx, dy, dz);

		Assert.Equal(expected, frame.HitFaceFrom(attackOrigin));
	}

	[Fact]
	public void HitFaceFrom_ZeroDelta_ReturnsFore()
	{
		var origin = new Coord(3, 3, 3);
		var frame = FrameAt(origin);

		Assert.Equal(ESpatialOrientation.Forward, frame.HitFaceFrom(origin));
	}

	[Fact]
	public void HitFaceFrom_TiedAxes_PrefersForeOverStarboard()
	{
		var origin = new Coord(0, 0, 0);
		var frame = FrameAt(origin);
		var attackOrigin = origin + new Coord(1, 0, 1);

		Assert.Equal(ESpatialOrientation.Forward, frame.HitFaceFrom(attackOrigin));
	}

	[Fact]
	public void HitFaceFrom_TiedForeAndPort_PrefersFore()
	{
		var origin = new Coord(0, 0, 0);
		var frame = FrameAt(origin);
		var attackOrigin = origin + new Coord(-1, 0, 1);

		Assert.Equal(ESpatialOrientation.Forward, frame.HitFaceFrom(attackOrigin));
	}

	[Fact]
	public void HitFaceFrom_TiedPortAndDorsal_PrefersPort()
	{
		var origin = new Coord(0, 0, 0);
		var frame = FrameAt(origin);
		var attackOrigin = origin + new Coord(-2, 2, 0);

		Assert.Equal(ESpatialOrientation.Port, frame.HitFaceFrom(attackOrigin));
	}
}

public sealed class ApplyDamageTests
{
	private static State FreshUnit() =>
		State.FromSpawn(new Instance { Id = "test", Type = EType.Fighter }, Coord.Zero);

	[Fact]
	public void OneDamageOnFullFace_ReducesShieldOnly()
	{
		var unit = FreshUnit();

		Defense.ApplyDamage(unit, 1, ESpatialOrientation.Forward);

		Assert.Equal(2, unit.HullPoints);
		Assert.Equal(1, unit.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(2, unit.ShieldPoints[ESpatialOrientation.Retro]);
	}

	[Fact]
	public void ThreeDamageOnFullFace_BleedsToHull()
	{
		var unit = FreshUnit();

		Defense.ApplyDamage(unit, 3, ESpatialOrientation.Forward);

		Assert.Equal(1, unit.HullPoints);
		Assert.Equal(0, unit.ShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void DamageOnDepletedFace_GoesStraightToHull()
	{
		var unit = FreshUnit();
		unit.ShieldPoints[ESpatialOrientation.Forward] = 0;

		Defense.ApplyDamage(unit, 1, ESpatialOrientation.Forward);

		Assert.Equal(1, unit.HullPoints);
		Assert.Equal(0, unit.ShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void LethalDamage_SetsHullToZero()
	{
		var unit = FreshUnit();
		unit.ShieldPoints.Fill(0);

		Defense.ApplyDamage(unit, 2, ESpatialOrientation.Forward);

		Assert.Equal(0, unit.HullPoints);
		Assert.False(unit.IsAlive);
	}

	[Fact]
	public void ZeroDamage_IsNoOp()
	{
		var unit = FreshUnit();

		Defense.ApplyDamage(unit, 0, ESpatialOrientation.Forward);

		Assert.Equal(2, unit.HullPoints);
		Assert.Equal(2, unit.ShieldPoints[ESpatialOrientation.Forward]);
	}
}
