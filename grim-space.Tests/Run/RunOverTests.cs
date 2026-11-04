using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Tests;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class RunOverTests
{
	[Fact]
	public void RemovingLastPilot_SetsRunOver()
	{
		using var run = RunState.CreateNewRun(42);
		Assert.False(run.IsRunOver);

		var fired = false;
		run.RunOverChanged += () => fired = true;

		foreach (var pilot in run.PilotRegistry.All.ToArray())
			run.PilotRegistry.Remove(pilot.Id);

		Assert.True(run.IsRunOver);
		Assert.True(fired);
		Assert.Empty(run.PilotRegistry.All);
	}

	[Fact]
	public void PartialPilotRemoval_DoesNotSetRunOver()
	{
		using var run = RunState.CreateNewRun(42);
		Assert.True(run.PilotRegistry.All.Count > 1);

		var first = run.PilotRegistry.All.First();
		run.PilotRegistry.Remove(first.Id);

		Assert.False(run.IsRunOver);
		Assert.NotEmpty(run.PilotRegistry.All);
	}

	[Fact]
	public void BattleLose_WithSurvivingPilot_DoesNotEndRun()
	{
		using var run = RunState.CreateNewRun(42);
		var survivorId = run.GetFieldedShipIds()[0];
		var doomedId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(doomedId, BattleUnitType.Fighter)));

		var engagementId = LaunchPirateEngagement(run);
		var outcome = new BattleOutcome(
			engagementId,
			EBattleResult.Lose,
			[
				OutcomeTestKit.Handoff(
					survivorId,
					run.ItemInventory.Get(survivorId).Spec.Chassis,
					1),
				OutcomeTestKit.Handoff(doomedId, BattleUnitType.Fighter, 0),
			]);

		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(outcome));

		Assert.Equal(EBattleResult.Lose, outcome.Result);
		Assert.False(run.IsRunOver);
		Assert.NotEmpty(run.PilotRegistry.All);
		Assert.True(run.PilotRegistry.TryGetForShip(survivorId, out _));
		Assert.False(run.PilotRegistry.TryGetForShip(doomedId, out _));
	}

	[Fact]
	public void BattleLose_WipingAllPilots_SetsRunOver()
	{
		using var run = RunState.CreateNewRun(42);
		var pilots = run.PilotRegistry.All.ToDictionary(pilot => pilot.Id, StringComparer.Ordinal);
		var shipIds = run.GetFieldedShipIds().ToArray();
		Assert.NotEmpty(shipIds);

		var engagementId = LaunchPirateEngagement(run);
		var spawns = run.ActiveBattle!.Spawns;
		var handoffs = shipIds
			.Select(id => OutcomeTestKit.Handoff(
				id,
				run.ItemInventory.Get(id).Spec.Chassis,
				0))
			.ToArray();
		var outcome = new BattleOutcome(engagementId, EBattleResult.Lose, handoffs);

		var fired = false;
		run.RunOverChanged += () => fired = true;
		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(outcome));

		Assert.True(run.IsRunOver);
		Assert.True(fired);
		Assert.Empty(run.PilotRegistry.All);

		var summary = BattleVictorySummaryBuilder.Build(
			outcome,
			spawns,
			PilotProgression.DefaultXpDifficulty,
			[],
			pilotId => run.PilotRegistry.TryGet(pilotId, out var pilot)
				? pilot
				: pilots.GetValueOrDefault(pilotId));

		Assert.Equal(shipIds.Length, summary.Pilots.Count);
		Assert.All(summary.Pilots, row =>
		{
			Assert.True(row.IsScrapped);
			Assert.Equal(0, row.ExperienceEarned);
			Assert.Contains(pilots.Values, pilot =>
				pilot.Name == row.DisplayName
				&& pilot.PortraitId == row.PortraitId
				&& pilot.ExperiencePoints == row.ExperiencePointsBefore);
		});
	}

	[Fact]
	public void SalaryDepart_RemovingLastPilot_SetsRunOver()
	{
		using var run = RunState.CreateNewRun(42);
		var pilots = run.PilotRegistry.All.ToArray();
		Assert.True(pilots.Length >= 2);

		for (var index = 0; index < pilots.Length - 1; index++)
			run.PilotRegistry.Remove(pilots[index].Id);
		Assert.False(run.IsRunOver);

		run.PilotRegistry.Remove(pilots[^1].Id);
		Assert.True(run.IsRunOver);
	}

	private static string LaunchPirateEngagement(RunState run)
	{
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		run.StarSystem.Map.FleetRegistry.Add(pirateFleet);
		foreach (var declaration in pirateFleet.Registrations)
			run.ItemInventory.Register(declaration, pirateFleet.State.Id);
		new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateFleet.State.Id)
			.Apply(
				run.StarSystem.Map,
				new GrimSpace.World.StarSystem.Runtime.ActorRuntime(),
				RunState.PlayerFleetUnitId);
		var engagementId = run.StarSystem.Map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement!.Id;
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var pilotSnapshots = new Dictionary<string, PilotBattleSnapshot>(StringComparer.Ordinal);
		foreach (var pilot in run.PilotRegistry.Active)
		{
			if (pilot.AssignedShipId is { } shipId)
				pilotSnapshots[shipId] = PilotBattleSnapshot.FromSkills(
					pilot.Id, pilot.LearnedSkills, pilot.PortraitId);
		}
		run.ActiveBattle = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ItemInventory,
			1,
			engagementId,
			playerFleet.State.Id,
			pilotSnapshots);
		return engagementId;
	}
}
