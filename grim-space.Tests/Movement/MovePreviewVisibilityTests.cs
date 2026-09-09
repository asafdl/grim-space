using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Movement;

public sealed class MovePreviewVisibilityTests
{
	[Fact]
	public void VisibleEndpointsMatchMovePathsAtRoot()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frame = BattleTestCommands.Frame(battle);

		var visible = VisibleEndpoints(frame.MovePaths, frame.MovePath, frame.MoveTarget);

		var expected = frame.MovePaths.Select(path => path.EndPosition).ToHashSet();
		if (frame.MoveTarget is Coord target)
			expected.Remove(target);

		Assert.Equal(expected, visible);
	}

	[Fact]
	public void VisibleEndpointsExcludeCommittedPathCells()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));

		var frame = BattleTestCommands.Frame(battle);
		var visible = VisibleEndpoints(frame.MovePaths, frame.MovePath, frame.MoveTarget);

		var expected = frame.MovePaths.Select(path => path.EndPosition).ToHashSet();
		expected.ExceptWith(frame.MovePath);

		Assert.Equal(expected, visible);
	}

	[Fact]
	public void VisibleEndpointsExcludeHoveredPathCells()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frame = BattleTestCommands.Frame(battle);

		for (var i = 0; i < frame.MovePaths.Count; i++)
		{
			var (path, target) = MoveUi.GetPathHighlights(frame.MovePaths, i, frame.CommittedMovePath);
			var visible = VisibleEndpoints(frame.MovePaths, path, target);
			var expected = frame.MovePaths.Select(option => option.EndPosition).ToHashSet();
			expected.ExceptWith(path);

			Assert.Equal(expected, visible);
		}
	}

	private static HashSet<Coord> VisibleEndpoints(
		IReadOnlyList<MovePathOption> paths,
		IReadOnlyList<Coord> path,
		Coord? target)
	{
		var pathCells = path.ToHashSet();
		var visible = new HashSet<Coord>();
		foreach (var option in paths)
		{
			if (option.EndPosition != target && !pathCells.Contains(option.EndPosition))
				visible.Add(option.EndPosition);
		}

		return visible;
	}
}
