using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Economy;

[StarSystemTestSuite]
public sealed class HazardPayEconomyTests
{
	[Fact]
	public void Engage_SpendsHazardPayAndClearsMisses()
	{
		using var run = RunState.CreateNewRun(42);
		DismissOpeningNarrative(run);
		var pilot = Assert.Single(run.PilotRegistry.Active);
		run.PilotRegistry.Update(pilot.WithHazardMissedPeriods(2));
		var creditsBefore = run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Credits);

		AddPirate(run);
		PrepareAwaitingDecision(run);
		run.StarSystem.PlayerAgent!.TryEnqueue([new EngageAction(RunState.PlayerFleetUnitId)]);
		run.StarSystem.AdvanceClock();

		Assert.Equal(
			creditsBefore - StrategicEconomyConfig.HazardPayCreditsPerPilot,
			run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out var after));
		Assert.Equal(0, after.MissedHazardPeriods);
		Assert.NotNull(run.ActiveBattle);
	}

	[Fact]
	public void EngagementFact_WithRefusedPilot_ExcludesShipAndFailsEmptyPlayerBattle()
	{
		using var run = RunState.CreateNewRun(42);
		DismissOpeningNarrative(run);
		var pilot = Assert.Single(run.PilotRegistry.Active);
		AddPirate(run);
		PrepareAwaitingDecision(run);

		var fact = new EngagementCommitted(
			"refuse-engagement",
			RunState.PlayerFleetUnitId,
			[RunState.PlayerFleetUnitId, "pirate-a"],
			RefusedPilotIds: [pilot.Id]);
		run.ReceiveEngagementFact(new Record<EngagementCommitted>(fact));

		Assert.Null(run.ActiveBattle);
		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out _));
	}

	private static void DismissOpeningNarrative(RunState run)
	{
		if (run.StarSystem.Map.ActiveNarrativeId is null)
			return;

		run.StarSystem.PlayerAgent!.TryEnqueue([
			new CompleteNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId),
		]);
		run.StarSystem.AdvanceClock();
	}

	private static void PrepareAwaitingDecision(RunState run)
	{
		var map = run.StarSystem.Map;
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, "pirate-a")
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new ReachContactEffect(RunState.PlayerFleetUnitId, "pirate-a")
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
	}

	private static void AddPirate(RunState run) =>
		run.StarSystem.Map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates));
}
