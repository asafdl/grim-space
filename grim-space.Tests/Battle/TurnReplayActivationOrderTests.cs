using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Core.Actions;

namespace GrimSpace.Tests;

[BattleTestSuite]
public sealed class TurnReplayActivationOrderTests
{
	[Fact]
	public void ActivationOrder_PreservesActivationOccurrencesAndSkipsSystemActors()
	{
		var replay = new TurnReplay(
			new Dictionary<string, GrimSpace.Battle.Units.State>(),
			[
				new EndOfPhaseAction("alpha"),
				new EndOfPhaseAction(BattleActorIds.Rules),
				new EndOfPhaseAction("beta"),
				new EndOfPhaseAction("alpha"),
				new EndOfPhaseAction(BattleActorIds.Terrain),
			],
			new Dictionary<string, GrimSpace.Battle.Units.State>());

		Assert.Equal(["alpha", "beta", "alpha"], replay.ActivationOrder);
	}

	[Fact]
	public void TurnFlowTimeline_BuildsTwoTurnsWithEndTurnAfterOnLastActivationOnly()
	{
		var timeline = TurnFlowTimeline.Build(
			[
				(IReadOnlyList<string>)["turn1-a", "turn1-b"],
				(IReadOnlyList<string>)["turn2-a", "turn2-b", "turn2-c"],
			],
			["turn3-a", "turn3-b"]);

		Assert.Equal(
			["turn1-a", "turn1-b", "turn2-a", "turn2-b", "turn2-c", "turn3-a", "turn3-b"],
			timeline.Select(entry => entry.UnitId));
		Assert.Equal([false, true, false, false, true, false, false],
			timeline.Select(entry => entry.EndTurnAfter));
		Assert.Equal(5, TurnFlowTimeline.StartOfCurrentSegment(timeline));
		Assert.True(TurnFlowTimeline.HasDividerAfter(timeline, 1));
		Assert.False(TurnFlowTimeline.HasDividerAfter(timeline, 2));
	}

	[Fact]
	public void TurnFlowTimeline_IndexOfUnit_FindsActivationIndex()
	{
		var timeline = TurnFlowTimeline.Build([], ["A", "B", "C"]);
		Assert.Equal(1, TurnFlowTimeline.IndexOfUnit(timeline, "B"));
		Assert.Equal(-1, TurnFlowTimeline.IndexOfUnit(timeline, "missing"));
	}

	[Fact]
	public void CenteredCarouselWindow_ClampsAndMapsVisibleItems()
	{
		var timeline = TurnFlowTimeline.Build([], ["A", "B", "C", "D", "E", "F", "G"]);
		var items = timeline.Select(entry => entry.UnitId).ToArray();

		Assert.Equal(0, CenteredCarouselWindow.ClampIndex(-1, items.Length));
		Assert.Equal(items.Length - 1, CenteredCarouselWindow.ClampIndex(99, items.Length));
		Assert.Null(CenteredCarouselWindow.ItemAt<string>(items, -1));
		Assert.Null(CenteredCarouselWindow.ItemAt<string>(items, items.Length));

		var centerIndex = 3;
		Assert.Equal(
			["B", "C", "D", "E", "F"],
			Enumerable.Range(0, 5)
				.Select(slot => CenteredCarouselWindow.ItemAt<string>(
					items,
					CenteredCarouselWindow.ItemIndexForSlot(centerIndex, slot, centerSlot: 2))));
	}
}
