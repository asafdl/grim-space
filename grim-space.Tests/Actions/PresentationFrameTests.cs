using GrimSpace.Battle;
using GrimSpace.Battle.Presentation.Planning;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class PresentationFrameTests
{
	[Fact]
	public void FrameAfterCommittedMoveShowsPostPlanEndpointsNotSelectionEndpoints()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var presenter = new BattlePresenter(battle);
		var options = View.GetLegalMoves(battle).ToList();
		var threeStepIndex = options.FindIndex(
			option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(presenter.TryQueueMove(threeStepIndex, options));

		var frame = presenter.BuildFrame();
		var endpoints = frame.MoveOptions.Select(option => option.EndPosition).ToHashSet();

		Assert.Equal(origin + Coord.Forward * 3, frame.ActorState.Position);
		Assert.DoesNotContain(origin + Coord.Forward * 4, endpoints);
		Assert.Equal(origin + Coord.Forward * 3, frame.MoveTarget);
		Assert.Equal(3, frame.MovePath.Count);
	}

	[Fact]
	public void UndoClearsCommittedMoveFromFrame()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var presenter = new BattlePresenter(battle);
		var options = View.GetLegalMoves(battle).ToList();
		var threeStepIndex = options.FindIndex(
			option => option.EndPosition == origin + Coord.Forward * 3);

		presenter.TryQueueMove(threeStepIndex, options);
		Assert.True(presenter.Undo());

		var frame = presenter.BuildFrame();

		Assert.Equal(origin, frame.ActorState.Position);
		Assert.Null(frame.MoveTarget);
		Assert.Empty(frame.MovePath);
		Assert.Contains(
			frame.MoveOptions,
			option => option.EndPosition == origin + Coord.Forward * 4);
	}

	private static BattleOrchestrator CreateOrchestrator(Coord playerPos, Coord enemyPos)
	{
		var encounter = new Encounter
		{
			Seed = 1,
			Spawns =
			[
				new Spawn
				{
					Unit = new Instance
					{
						Id = "player",
						Type = EType.Fighter,
						Controller = EController.Player,
					},
					Position = playerPos,
				},
				new Spawn
				{
					Unit = new Instance
					{
						Id = "enemy",
						Type = EType.Fighter,
						Controller = EController.Enemy,
					},
					Position = enemyPos,
				},
			],
		};

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
	}
}
