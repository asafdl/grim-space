using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Round;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.ActivationRound;

[BattleTestSuite]
public sealed class RoundExecutionAgentTests
{
	private sealed record MarkerAction(string ActorId, int Id) : IAction;

	private const string PlayerId = "player";

	[Fact]
	public void PlanAndPublish_stickyStartPublishesMarkerAndListActions()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var world = battle.Engine.World;
		world.ActivationRoundLists.Append(
			PlayerId,
			new ActivationRoundEntry(
				EUnitActivationPhase.Start,
				RoundsRemaining: null,
				Sticky: true,
				new MarkerAction(PlayerId, 1)));

		var (agent, sink) = CreateAgent(world);

		agent.PlanAndPublish(PlayerId, EUnitActivationPhase.Start);
		Assert.True(sink.TryTakeBatch(BattleActorIds.Rules, out var first));
		Assert.IsType<ProcessActivationRoundAction>(first.Actions[0]);
		Assert.IsType<MarkerAction>(first.Actions[1]);

		agent.PlanAndPublish(PlayerId, EUnitActivationPhase.Start);
		Assert.True(sink.TryTakeBatch(BattleActorIds.Rules, out var second));
		Assert.Equal(2, second.Actions.Count);
		Assert.Single(world.ActivationRoundLists.EntriesFor(PlayerId));
	}

	[Fact]
	public void PlanAndPublish_countdownEndPopsAfterNPasses()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var world = battle.Engine.World;
		var runtime = ActorRuntime.From(null);
		var release = new MarkerAction(PlayerId, 99);
		new ScheduleActivationRoundEffect(EUnitActivationPhase.End, 2, sticky: false, release)
			.Apply(world, runtime, PlayerId);

		var (agent, sink) = CreateAgent(world);

		agent.PlanAndPublish(PlayerId, EUnitActivationPhase.End);
		Assert.True(sink.TryTakeBatch(BattleActorIds.Rules, out var tick));
		Assert.IsType<ProcessActivationRoundAction>(Assert.Single(tick.Actions));

		agent.PlanAndPublish(PlayerId, EUnitActivationPhase.End);
		Assert.True(sink.TryTakeBatch(BattleActorIds.Rules, out var batch));
		Assert.IsType<ProcessActivationRoundAction>(batch.Actions[0]);
		Assert.Same(release, batch.Actions[1]);
		Assert.Empty(world.ActivationRoundLists.EntriesFor(PlayerId));
	}

	[Fact]
	public void PlanAndPublish_emptyListStillPublishesMarker()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var (agent, sink) = CreateAgent(battle.Engine.World);
		agent.PlanAndPublish(PlayerId, EUnitActivationPhase.End);
		Assert.True(sink.TryTakeBatch(BattleActorIds.Rules, out var batch));
		Assert.IsType<ProcessActivationRoundAction>(Assert.Single(batch.Actions));
	}

	[Fact]
	public void PlanAndPublish_commitEmitsPhaseRecord()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var (agent, sink) = CreateAgent(battle.Engine.World);
		agent.PlanAndPublish(PlayerId, EUnitActivationPhase.Start);
		Assert.True(sink.TryTakeBatch(BattleActorIds.Rules, out var batch));
		battle.Engine.Commit([.. batch.Actions]);

		var record = battle.Engine.History().OfType<Record<UnitActivationRoundPhase>>().Single();
		Assert.Equal(PlayerId, record.Value.UnitId);
		Assert.Equal(EUnitActivationPhase.Start, record.Value.Phase);
	}

	[Fact]
	public void PlanAndPublish_runAtEndIgnoresStartScheduledRowButStillMarksPhase()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		battle.Engine.World.ActivationRoundLists.Append(
			PlayerId,
			new ActivationRoundEntry(
				EUnitActivationPhase.Start,
				RoundsRemaining: null,
				Sticky: true,
				new MarkerAction(PlayerId, 1)));

		var (agent, sink) = CreateAgent(battle.Engine.World);
		agent.PlanAndPublish(PlayerId, EUnitActivationPhase.End);
		Assert.True(sink.TryTakeBatch(BattleActorIds.Rules, out var batch));
		Assert.IsType<ProcessActivationRoundAction>(Assert.Single(batch.Actions));
	}

	private static (RoundExecutionAgent Agent, ActionBatchSink Sink) CreateAgent(BattleWorld world)
	{
		var sink = new ActionBatchSink();
		var agent = new RoundExecutionAgent(() => world);
		agent.Init(BattleActorIds.Rules, sink.WriterFor(BattleActorIds.Rules));
		agent.SetCanWork(true);
		return (agent, sink);
	}
}
