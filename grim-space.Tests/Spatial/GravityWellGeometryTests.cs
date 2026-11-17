using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using System.Collections.Frozen;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests.Spatial;

[BattleTestSuite]
public sealed class GravityWellGeometryTests
{
	private static readonly BoundedGrid BattleGrid = new(20, 20, 20);
	private static readonly Coord Center = new(10, 10, 10);
	private static readonly Coord Right = new(1, 0, 0);

	[Fact]
	public void AssistsThreeEightCellRingsAroundTheDorsalAxis()
	{
		var well = CreateWell();

		Assert.Equal(27, well.Cells.Count);
		Assert.Equal(24, well.AssistCells.Count);
		Assert.Equal(Center, well.CoreCell);
		Assert.True(well.AssistCells.SetEquals(well.AssistedDirectionsByCell.Keys));
		foreach (var unassisted in new[] { Center, Center + Coord.Up, Center - Coord.Up })
			Assert.DoesNotContain(unassisted, well.AssistCells.ToArray());
		Assert.All(new[] { -1, 0, 1 }, layer =>
			Assert.Equal(8, well.AssistCells.Count(cell => cell.Y - Center.Y == layer)));
	}

	[Fact]
	public void RingCellsAssistTheSpinHeadingAndCornersKeepTheirIncomingHeading()
	{
		var well = CreateWell();
		var side = Center + Right;
		var corner = Center + Right + Coord.Forward;

		Assert.True(new[] { Coord.Forward }.ToHashSet().SetEquals(well.AssistedDirectionsByCell[side]));
		Assert.True(new[] { Coord.Forward }.ToHashSet().SetEquals(well.AssistedDirectionsByCell[side + Coord.Up]));
		Assert.True(new[] { Coord.Forward, -Right }.ToHashSet().SetEquals(well.AssistedDirectionsByCell[corner]));

		Assert.Equal(
			new[] { new GravityWellFace(side, Right, Coord.Forward) },
			well.Faces.Where(face => face.Cell == side));
		Assert.True(new[]
		{
			new GravityWellFace(corner, Right, Coord.Forward),
			new GravityWellFace(corner, Coord.Forward, -Right),
		}.ToHashSet().SetEquals(well.Faces.Where(face => face.Cell == corner)));
		Assert.All(well.Faces, face => Assert.Equal(0, Coord.Dot(face.Normal, face.Heading)));
	}

	[Fact]
	public void MirroredCellsHaveNegatedAssistanceInEveryCardinalOrientation()
	{
		var axes = new[] { Right, -Right, Coord.Up, -Coord.Up, Coord.Forward, -Coord.Forward };
		foreach (var fore in axes)
		foreach (var dorsal in axes)
		{
			if (Coord.Dot(fore, dorsal) != 0)
				continue;

			var frame = new BodyFrame(Center, fore, dorsal, Coord.Cross(dorsal, fore));
			var well = GravityWell.Create("gravitywell-test", Center, frame, BattleGrid);
			Assert.Equal(27, well.Cells.Count);

			foreach (var cell in well.AssistCells)
			{
				var mirroredCell = Center * 2 - cell;
				Assert.Contains(mirroredCell, well.AssistCells.ToArray());
				Assert.True(well.AssistedDirectionsByCell[cell]
					.Select(direction => -direction)
					.ToHashSet()
					.SetEquals(well.AssistedDirectionsByCell[mirroredCell]));
			}
		}
	}

	[Fact]
	public void OnlyCoreBlocksMovementAndAbilityLinesAcrossForks()
	{
		var well = CreateWell();
		var origin = Center - Coord.Forward;
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			[BattleTestFixture.Enemy(new Coord(2, 2, 2))],
			BattleGrid,
			BattleWorld.TerrainBlockedCells([well]),
			hazards: [well]);
		var world = battle.Engine.World;
		var fork = world.Fork();

		Assert.Same(well, fork.NonUnits[well.Id]);
		Assert.Equal(new[] { Center }, well.MovementBlockingCells);
		Assert.Equal(new[] { Center }, well.AbilityBlockingCells);
		Assert.True(fork.IsCellBlocked(Center));
		Assert.All(well.AssistCells.Where(cell => cell != origin), cell => Assert.False(fork.IsCellBlocked(cell)));
		Assert.False(battle.Engine.CreateSimulation().TryEnqueue(new MoveStepAction("player")));

		var passableBattle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin + Right),
			[BattleTestFixture.Enemy(new Coord(2, 2, 2))],
			BattleGrid,
			BattleWorld.TerrainBlockedCells([well]),
			hazards: [well]);
		Assert.True(passableBattle.Engine.CreateSimulation().TryEnqueue(new MoveStepAction("player")));

		var throughCore = new[] { Center, Center + Coord.Forward }.ToFrozenSet();
		var throughAssist = new[] { Center + Right, Center + Right + Coord.Forward }.ToFrozenSet();
		var coreResult = AbilityArea.ApplyBlocking(origin, throughCore, true, fork.AbilityBlockingCells);
		var assistResult = AbilityArea.ApplyBlocking(
			origin + Right, throughAssist, true, fork.AbilityBlockingCells);
		Assert.Empty(coreResult);
		Assert.Equal(throughAssist.Count, assistResult.Count);
	}

	[Fact]
	public void RejectsInvalidFrameAndClippedVolume()
	{
		Assert.Throws<ArgumentException>(() =>
			GravityWell.Create(
				"gravitywell-invalid-frame",
				Center,
				new BodyFrame(Center, Coord.Forward, Coord.Up, Coord.Forward),
				BattleGrid));
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			GravityWell.Create(
				"gravitywell-clipped",
				new Coord(0, 0, 0),
				BodyFrame.WorldAligned(new Coord(0, 0, 0)),
				BattleGrid));
	}

	private static GravityWell CreateWell() =>
		CreateWell(BodyFrame.WorldAligned(Center));

	private static GravityWell CreateWell(BodyFrame frame) =>
		GravityWell.Create(
			"gravitywell-test",
			Center,
			frame,
			BattleGrid);
}
