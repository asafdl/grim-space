using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation;
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
		var ui = BattleTestFixture.Ui(battle);
		var frame = ui.BuildFrame();

		var visible = VisibleEndpoints(frame.MovePaths, frame.MovePathApBaseline, frame.MoveTarget);

		var expected = frame.MovePaths.Select(path => path.EndPosition).ToHashSet();
		if (frame.MoveTarget is Coord target)
			expected.Remove(target);

		Assert.Equal(expected, visible);
	}

	[Fact]
	public void VisibleEndpointsMatchMovePathsAfterCommittedMove()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		var threeStep = ui.MoveUi.GetMovePaths([])
			.First(path => path.EndPosition == origin + Coord.Forward * 3);

		Assert.True(ui.TryQueueMove(threeStep.EndPosition));

		var frame = ui.BuildFrame();
		var visible = VisibleEndpoints(frame.MovePaths, frame.MovePathApBaseline, frame.MoveTarget);

		var expected = frame.MovePaths.Select(path => path.EndPosition).ToHashSet();
		if (frame.MoveTarget is Coord target)
			expected.Remove(target);

		Assert.Equal(expected, visible);
	}

	[Fact]
	public void VisibleEndpointsMatchMovePathsWhileHoveringEachOption()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		var frame = ui.BuildFrame();

		for (var i = 0; i < frame.MovePaths.Count; i++)
		{
			var (_, target) = MoveUi.GetPathHighlights(frame.MovePaths, i, ui.State.CommittedMovePath);
			var visible = VisibleEndpoints(frame.MovePaths, frame.MovePathApBaseline, target);
			var expected = frame.MovePaths.Select(option => option.EndPosition).ToHashSet();
			if (target is Coord hoveredEnd)
				expected.Remove(hoveredEnd);

			Assert.Equal(expected, visible);
		}
	}

	private static HashSet<Coord> VisibleEndpoints(
		IReadOnlyList<MovePathSession> paths,
		int pathApBaseline,
		Coord? target)
	{
		var endpointAp = new Dictionary<Coord, int>();
		foreach (var session in paths)
			endpointAp[session.EndPosition] = session.ExtensionApCost(pathApBaseline);

		var visible = new HashSet<Coord>();
		foreach (var (coord, _) in endpointAp)
		{
			if (coord != target)
				visible.Add(coord);
		}

		return visible;
	}
}
