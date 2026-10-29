using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.ActivationRound;

[BattleTestSuite]
public sealed class ActivationRoundListTests
{
	private sealed record MarkerAction(string ActorId, int Id) : IAction;

	private const string PlayerId = "player";

	// Scheduling does not decrement; the first End pass after schedule is the first tick toward N.
	[Fact]
	public void CountdownAtEnd_firesAndRemovesAfterNEndPasses()
	{
		var world = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).Engine.World;
		var lists = world.ActivationRoundLists;
		var runtime = ActorRuntime.From(null);
		const int duration = 2;
		var release = new MarkerAction(PlayerId, 99);
		var schedule = new ScheduleActivationRoundEffect(EUnitActivationPhase.End, duration, sticky: false, release);

		schedule.Apply(world, runtime, PlayerId);
		Assert.Single(lists.EntriesFor(PlayerId));
		Assert.Equal(duration, lists.EntriesFor(PlayerId)[0].RoundsRemaining);

		Assert.Empty(lists.ProcessPass(PlayerId, EUnitActivationPhase.End));
		Assert.Equal(1, lists.EntriesFor(PlayerId)[0].RoundsRemaining);

		var published = lists.ProcessPass(PlayerId, EUnitActivationPhase.End);
		Assert.Single(published);
		Assert.Same(release, published[0]);
		Assert.Empty(lists.EntriesFor(PlayerId));
	}

	[Fact]
	public void ScheduleEffect_undoRestoresList()
	{
		var world = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).Engine.World;
		var lists = world.ActivationRoundLists;
		var runtime = ActorRuntime.From(null);
		var effect = new ScheduleActivationRoundEffect(
			EUnitActivationPhase.End,
			roundsRemaining: 3,
			sticky: false,
			new MarkerAction(PlayerId, 1));

		effect.Apply(world, runtime, PlayerId);
		Assert.Single(lists.EntriesFor(PlayerId));

		effect.Undo(world, runtime, PlayerId);
		Assert.Empty(lists.EntriesFor(PlayerId));
	}

	[Fact]
	public void StickyRecurringAtStart_publishesEachStartPassAndStaysOnList()
	{
		var world = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).Engine.World;
		var lists = world.ActivationRoundLists;
		lists.Append(
			PlayerId,
			new ActivationRoundEntry(
				EUnitActivationPhase.Start,
				RoundsRemaining: null,
				Sticky: true,
				new MarkerAction(PlayerId, 7)));

		var first = lists.ProcessPass(PlayerId, EUnitActivationPhase.Start);
		var second = lists.ProcessPass(PlayerId, EUnitActivationPhase.Start);

		Assert.Single(first);
		Assert.Single(second);
		Assert.Single(lists.EntriesFor(PlayerId));
	}

	[Fact]
	public void RunAt_filtersStartVersusEndPasses()
	{
		var world = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).Engine.World;
		var lists = world.ActivationRoundLists;
		lists.Append(
			PlayerId,
			new ActivationRoundEntry(
				EUnitActivationPhase.Start,
				RoundsRemaining: null,
				Sticky: true,
				new MarkerAction(PlayerId, 1)));

		Assert.Empty(lists.ProcessPass(PlayerId, EUnitActivationPhase.End));
		Assert.Single(lists.ProcessPass(PlayerId, EUnitActivationPhase.Start));
	}

	[Fact]
	public void Fork_copiesActivationRoundListsIndependently()
	{
		var world = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).Engine.World;
		var lists = world.ActivationRoundLists;
		lists.Append(
			PlayerId,
			new ActivationRoundEntry(
				EUnitActivationPhase.End,
				RoundsRemaining: 2,
				Sticky: false,
				new MarkerAction(PlayerId, 1)));

		var fork = world.Fork();
		var forkLists = fork.ActivationRoundLists;
		Assert.Single(forkLists.EntriesFor(PlayerId));
		Assert.Equal(2, forkLists.EntriesFor(PlayerId)[0].RoundsRemaining);

		Assert.Empty(lists.ProcessPass(PlayerId, EUnitActivationPhase.End));
		Assert.Equal(1, lists.EntriesFor(PlayerId)[0].RoundsRemaining);
		Assert.Equal(2, forkLists.EntriesFor(PlayerId)[0].RoundsRemaining);

		forkLists.ProcessPass(PlayerId, EUnitActivationPhase.End);
		Assert.Equal(1, lists.EntriesFor(PlayerId)[0].RoundsRemaining);
		Assert.Equal(1, forkLists.EntriesFor(PlayerId)[0].RoundsRemaining);
	}
}
