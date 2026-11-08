using GrimSpace.Run.Persistence;
using GrimSpace.Tests.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Events;

[StarSystemTestSuite]
public sealed class WorldEventScheduleTests
{
	[Fact]
	public void BeforeWake_NothingIsScheduled()
	{
		using var run = RunTestKit.CreateFighterSupportRun();

		AdvanceTickTo(run.StarSystem, WorldEventConfig.WakeCadenceTicks - 1);

		Assert.Equal(0, PendingRolls(run.StarSystem.Map));
	}

	[Fact]
	public void Wake_TopsUpToTargetWithFutureRolls()
	{
		using var run = RunTestKit.CreateFighterSupportRun();

		AdvanceTickTo(run.StarSystem, WorldEventConfig.WakeCadenceTicks);

		Assert.Equal(WorldEventConfig.TargetScheduledRolls, PendingRolls(run.StarSystem.Map));
		foreach (var tick in PendingRollTicks(run.StarSystem.Map))
		{
			Assert.InRange(
				tick,
				WorldEventConfig.WakeCadenceTicks + 1,
				WorldEventConfig.WakeCadenceTicks * 2);
		}
	}

	[Fact]
	public void Wake_WithoutActivePilots_SchedulesNothing()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		foreach (var pilot in run.PilotRegistry.Active.ToArray())
			run.PilotRegistry.Deactivate(pilot.Id);

		AdvanceTickTo(run.StarSystem, WorldEventConfig.WakeCadenceTicks);

		Assert.Equal(0, PendingRolls(run.StarSystem.Map));
	}

	[Fact]
	public void Wake_ClockAdvancementPathAlsoSchedules()
	{
		using var run = RunTestKit.CreateFighterSupportRun();

		while (run.StarSystem.Tick < WorldEventConfig.WakeCadenceTicks)
			run.StarSystem.AdvanceClock();

		Assert.Equal(WorldEventConfig.TargetScheduledRolls, PendingRolls(run.StarSystem.Map));
	}

	[Fact]
	public void NextWake_ReplenishesAfterRollsFire()
	{
		using var run = RunTestKit.CreateFighterSupportRun();

		AdvanceTickTo(run.StarSystem, WorldEventConfig.WakeCadenceTicks * 2);

		Assert.Equal(WorldEventConfig.TargetScheduledRolls, PendingRolls(run.StarSystem.Map));
		foreach (var tick in PendingRollTicks(run.StarSystem.Map))
			Assert.True(tick > WorldEventConfig.WakeCadenceTicks * 2);
	}

	[Fact]
	public void Schedule_IsDeterministicForSeed()
	{
		using var first = RunTestKit.CreateFighterSupportRun();
		using var second = RunTestKit.CreateFighterSupportRun();

		AdvanceTickTo(first.StarSystem, WorldEventConfig.WakeCadenceTicks);
		AdvanceTickTo(second.StarSystem, WorldEventConfig.WakeCadenceTicks);

		Assert.Equal(PendingRollTicks(first.StarSystem.Map), PendingRollTicks(second.StarSystem.Map));
	}

	[Fact]
	public void Maintain_RejectsOverfillingTheTarget()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		AdvanceTickTo(run.StarSystem, WorldEventConfig.WakeCadenceTicks);
		var map = run.StarSystem.Map;

		var action = new MaintainWorldEventScheduleAction(
			StarSystemActorIds.WorldEvents,
			map.Timeline.Clock.Current,
			[new WorldEventSlot(EWorldEventId.AsteroidAccident, 10, 1)]);

		Assert.False(MaintainWorldEventScheduleDef.Instance.IsLegal(
			action,
			map,
			run.StarSystem.RuntimeFor(StarSystemActorIds.WorldEvents)));
	}

	[Fact]
	public void Fork_PreservesScheduledRolls()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		AdvanceTickTo(run.StarSystem, WorldEventConfig.WakeCadenceTicks);

		var fork = run.StarSystem.Map.ForkForSimulation();

		Assert.Equal(PendingRollTicks(run.StarSystem.Map), PendingRollTicks(fork));
	}

	[Fact]
	public void SaveLoad_PreservesScheduledRolls()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		AdvanceTickTo(run.StarSystem, WorldEventConfig.WakeCadenceTicks);
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.Equal(PendingRollTicks(run.StarSystem.Map), PendingRollTicks(restored.StarSystem.Map));
		Assert.Equal(WorldEventConfig.TargetScheduledRolls, PendingRolls(restored.StarSystem.Map));
	}

	private static void AdvanceTickTo(StarSystemOrchestrator orchestrator, int tick) =>
		orchestrator.AdvanceTicks(tick - orchestrator.Tick);

	private static int PendingRolls(StarMap map) =>
		map.Timeline.CountPending(action => action is IWorldEventRoll);

	private static int[] PendingRollTicks(StarMap map) =>
		map.Timeline.ToSnapshot().Pending
			.SelectMany(pair => pair.Value
				.Where(action => action is IWorldEventRoll)
				.Select(_ => pair.Key))
			.Order()
			.ToArray();
}
