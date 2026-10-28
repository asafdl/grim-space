using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using RunState = GrimSpace.Run.State;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.PilotCapabilities;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Runtime;

[StarSystemTestSuite]
public sealed class StrategicActorRuntimeTests(StarMapFixture maps)
{
	[Fact]
	public void NpcRuntime_KeepsEmptyPipeline()
	{
		var map = maps.Fresh(2);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var npcId = map.FleetRegistry.Ids.First(id => id != RunState.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, RunState.PlayerFleetUnitId);
		orchestrator.RuntimeFor(RunState.PlayerFleetUnitId).ReplaceActionEffects(
			StarMapPilotCapabilityPipelineFactory.Build(
				StackGrants(EPilotCapabilityId.ScrapRewardBonus, 1)));

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
			StarMapPilotCapabilityPipelineFactory.Build(
				StackGrants(EPilotCapabilityId.ScrapRewardBonus, 1)));
		var live = orchestrator.RuntimeFor(RunState.PlayerFleetUnitId);
		var sim = orchestrator.CreateSimulation();
		var preview = sim.RuntimeFor(RunState.PlayerFleetUnitId);

		Assert.Same(live.ActionEffects, preview.ActionEffects);
	}

	[Fact]
	public void RefreshStrategicPilotCapabilities_ReplacesPipelineAndUndoes()
	{
		var map = maps.Fresh(4);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, RunState.PlayerFleetUnitId);
		var runtime = orchestrator.RuntimeFor(RunState.PlayerFleetUnitId);
		var before = runtime.ActionEffects;
		var grants = StackGrants(EPilotCapabilityId.ScrapRewardBonus, 2);

		var effect = new ReplaceStrategicActionEffectPipelineEffect(grants);
		effect.Apply(orchestrator.Map, runtime, RunState.PlayerFleetUnitId);
		var after = runtime.ActionEffects;
		Assert.NotSame(before, after);

		effect.Undo(orchestrator.Map, runtime, RunState.PlayerFleetUnitId);
		Assert.Same(before, runtime.ActionEffects);
	}

	[Fact]
	public void Reset_PreservesActionEffects()
	{
		var runtime = new ActorRuntime();
		var pipeline = StarMapPilotCapabilityPipelineFactory.Build(
			StackGrants(EPilotCapabilityId.ScrapRewardBonus, 1));
		runtime.ReplaceActionEffects(pipeline);
		runtime.CachedPath = new GrimSpace.World.StarSystem.Pathfinding.TransitPath(
			System.Collections.Immutable.ImmutableArray<GrimSpace.World.StarSystem.Pathfinding.TransitLeg>.Empty);
		runtime.Reset();

		Assert.Same(pipeline, runtime.ActionEffects);
		Assert.Null(runtime.CachedPath);
	}

	private static PilotCapabilityGrant[] StackGrants(
		EPilotCapabilityId capabilityId,
		int stacks)
	{
		var grants = new PilotCapabilityGrant[stacks];
		for (var index = 0; index < stacks; index++)
		{
			grants[index] = new PilotCapabilityGrant(
				capabilityId,
				$"pilot-{index}",
				new PilotCapabilitySource(EPilotCapabilitySourceKind.Skill, "test-skill"));
		}
		return grants;
	}
}
