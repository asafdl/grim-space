using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Tests.Actions;

public sealed class TimelineTests
{
	[Fact]
	public void ScheduleEnqueuesAtCurrentPlusDelay()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(3);
		var action = new HeadingTurnAction("player", EHeadingTurn.YawRight);

		timeline.Schedule(2, action);

		Assert.Single(timeline.At(5).Snapshot());
		Assert.Equal(action, timeline.At(5).Snapshot()[0]);
	}

	[Fact]
	public void ClonePreservesPendingBuckets()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(1);
		var action = new HeadingTurnAction("player", EHeadingTurn.YawRight);
		timeline.Schedule(1, action);

		var clone = timeline.Clone();

		Assert.Equal(1, clone.Clock.Current);
		Assert.Single(clone.SnapshotAt(2));
	}

	[Fact]
	public void FromReturnsActionsInTickOrder()
	{
		var timeline = new Timeline();
		timeline.At(2).Enqueue(new HeadingTurnAction("a", Battle.Movement.Enums.EHeadingTurn.YawRight));
		timeline.At(4).Enqueue(new HeadingTurnAction("b", EHeadingTurn.YawLeft));

		var entries = timeline.From(2).ToList();

		Assert.Equal(2, entries.Count);
		Assert.Equal(2, entries[0].Tick);
		Assert.Equal("a", entries[0].Action.OwnerId);
		Assert.Equal(4, entries[1].Tick);
	}
}
