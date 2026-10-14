using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using RunState = GrimSpace.Run.State;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.PilotSkills;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Skills;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Runtime;

[StarSystemTestSuite]
public sealed class StrategicActorRuntimeTests(StarMapFixture maps)
{
	[Fact]
	public void PlayerRuntime_InstallsPipelineFromStrategicGrants()
	{
		var map = maps.Fresh(1);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, RunState.PlayerFleetUnitId);
		var grants = StackGrants(EPilotSkillId.IKnowABot, 1)
			.Concat(StackGrants(EPilotSkillId.DronesYearnForMines, 2))
			.ToArray();
		orchestrator.RuntimeFor(RunState.PlayerFleetUnitId).ReplaceActionEffects(
			StarMapActionEffectPipelineFactory.Build(grants));

		var runtimeGrants = StarMapActionEffectPipelineFactory.GrantsFromPipeline(
			orchestrator.RuntimeFor(RunState.PlayerFleetUnitId).ActionEffects);

		Assert.Equal(1, StrategicPilotSkillModifiers.Count(runtimeGrants, EPilotSkillId.IKnowABot));
		Assert.Equal(2, StrategicPilotSkillModifiers.Count(runtimeGrants, EPilotSkillId.DronesYearnForMines));
	}

	[Fact]
	public void NpcRuntime_KeepsEmptyPipeline()
	{
		var map = maps.Fresh(2);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var npcId = map.FleetRegistry.Ids.First(id => id != RunState.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, RunState.PlayerFleetUnitId);
		orchestrator.RuntimeFor(RunState.PlayerFleetUnitId).ReplaceActionEffects(
			StarMapActionEffectPipelineFactory.Build(StackGrants(EPilotSkillId.IKnowABot, 1)));

		var npcRuntime = orchestrator.RuntimeFor(npcId);
		Assert.True(npcRuntime.ActionEffects.IsEmpty);
	}

	[Fact]
	public void Fork_SharesPipelineReference()
	{
		var map = maps.Fresh(3);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, RunState.PlayerFleetUnitId);
		orchestrator.RuntimeFor(RunState.PlayerFleetUnitId).ReplaceActionEffects(
			StarMapActionEffectPipelineFactory.Build(StackGrants(EPilotSkillId.IKnowABot, 1)));
		var live = orchestrator.RuntimeFor(RunState.PlayerFleetUnitId);
		var sim = orchestrator.CreateSimulation();
		var preview = sim.RuntimeFor(RunState.PlayerFleetUnitId);

		Assert.Same(live.ActionEffects, preview.ActionEffects);
	}

	[Fact]
	public void RefreshStrategicPilotSkills_ReplacesPipelineAndUndoes()
	{
		var map = maps.Fresh(4);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, RunState.PlayerFleetUnitId);
		var runtime = orchestrator.RuntimeFor(RunState.PlayerFleetUnitId);
		var before = runtime.ActionEffects;
		var grants = StackGrants(EPilotSkillId.IKnowABot, 2)
			.Concat(StackGrants(EPilotSkillId.DronesYearnForMines, 1))
			.ToArray();

		var effect = new ReplaceStrategicActionEffectPipelineEffect(grants);
		effect.Apply(orchestrator.Map, runtime, RunState.PlayerFleetUnitId);
		var after = runtime.ActionEffects;
		Assert.NotSame(before, after);
		Assert.Equal(
			2,
			StrategicPilotSkillModifiers.Count(
				StarMapActionEffectPipelineFactory.GrantsFromPipeline(after),
				EPilotSkillId.IKnowABot));

		effect.Undo(orchestrator.Map, runtime, RunState.PlayerFleetUnitId);
		Assert.Same(before, runtime.ActionEffects);
	}

	[Fact]
	public void Reset_PreservesActionEffects()
	{
		var runtime = new ActorRuntime();
		var pipeline = StarMapActionEffectPipelineFactory.Build(StackGrants(EPilotSkillId.IKnowABot, 1));
		runtime.ReplaceActionEffects(pipeline);
		runtime.CachedPath = new GrimSpace.World.StarSystem.Pathfinding.TransitPath(
			System.Collections.Immutable.ImmutableArray<GrimSpace.World.StarSystem.Pathfinding.TransitLeg>.Empty);
		runtime.Reset();

		Assert.Same(pipeline, runtime.ActionEffects);
		Assert.Null(runtime.CachedPath);
	}

	private static PilotSkillGrant[] StackGrants(EPilotSkillId skillId, int stacks)
	{
		var grants = new PilotSkillGrant[stacks];
		for (var index = 0; index < stacks; index++)
			grants[index] = new PilotSkillGrant(skillId, $"pilot-{index}");
		return grants;
	}
}
