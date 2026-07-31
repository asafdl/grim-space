using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Movement;

public sealed class MoveUiTests
{
	private static readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] MovementActionDefs =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];
	[Fact]
	public void MoveOptionsMatchEmptyQueueAtTurnStart()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		var fromCache = battle.MoveUi.GetMoveOptions([]);
		var fromFacade = MoveUi.GetMoveOptions(battle, battle.GetActiveActor());

		Assert.Equal(
			fromFacade.Select(option => option.EndPosition).OrderBy(coord => coord.X),
			fromCache.Select(option => option.EndPosition).OrderBy(coord => coord.X));
	}

	[Fact]
	public void SearchIncludesHeadingOnlyBranches()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		var headingFrames = battle.Sim
			.Search(battle.PlayerId, MovementActionDefs, BattleSearchVisit.ForCapabilities)
			.Where(frame => frame.Actions is [HeadingTurnAction])
			.ToList();

		Assert.NotEmpty(headingFrames);
	}

	[Fact]
	public void MoveOptionsAfterHeadingUseCachedTreeWithoutResearch()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		var beforeHeading = battle.MoveUi.GetMoveOptions([]).ToList();
		Assert.NotEmpty(beforeHeading);

		var yawRight = new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight);
		Assert.True(battle.MoveUi.TryLocate([yawRight], out _));

		Assert.True(battle.Sim.TryEnqueue(yawRight));

		var enqueued = Assert.IsType<HeadingTurnAction>(Assert.Single(battle.Sim.Actions));
		Assert.Equal(EHeadingTurn.YawRight, enqueued.Turn);

		var matchingFrame = battle.Sim
			.Search(battle.PlayerId, MovementActionDefs, BattleSearchVisit.ForCapabilities)
			.First(frame => frame.Actions is [HeadingTurnAction { Turn: EHeadingTurn.YawRight }]);

		Assert.True(battle.MoveUi.TryLocate(battle.Sim.Actions, out _));

		var afterHeading = battle.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		Assert.NotEmpty(afterHeading);
		Assert.NotEqual(
			beforeHeading.Select(option => option.EndPosition).ToHashSet(),
			afterHeading.Select(option => option.EndPosition).ToHashSet());
	}

	[Fact]
	public void MoveOptionsEmptyWhenMovePathStarted()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		var move = battle.MoveUi.GetMoveOptions([])
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));

		Assert.Empty(battle.MoveUi.GetMoveOptions(battle.Sim.Actions));
	}
}
