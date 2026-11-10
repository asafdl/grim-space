using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Tests.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Resources;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Events;

[StarSystemTestSuite]
public sealed class CompulsiveStakeTests
{
	[Theory]
	[InlineData(-5, 0.20)]
	[InlineData(0, 0)]
	[InlineData(5, 0)]
	public void TriggerChance_RisesAsDeliberationFalls(int deliberation, double expected) =>
		Assert.Equal(
			expected,
			RollForCompulsiveStakeDef.TriggerChance(PilotWith(deliberation)),
			precision: 10);

	[Theory]
	[InlineData(-5, 0.18, 0.22)]
	[InlineData(5, 0, 0)]
	public void SelectDemander_TriggersAtPersonalityAdjustedRate(
		int deliberation,
		double minRate,
		double maxRate)
	{
		var pilots = new[] { PilotWith(deliberation) };

		var triggers = Enumerable.Range(0, 5_000)
			.Count(seed => RollForCompulsiveStakeDef.SelectDemander((ulong)seed, pilots) is not null);

		Assert.InRange(triggers / 5_000.0, minRate, maxRate);
	}

	[Fact]
	public void RollStake_StaysInsideTheBand()
	{
		var stakes = Enumerable.Range(0, 2_000)
			.Select(seed => RollForCompulsiveStakeDef.RollStake((ulong)seed))
			.ToArray();

		Assert.All(stakes, stake =>
			Assert.InRange(stake, WorldEventCatalog.Stake.CreditsMin, WorldEventCatalog.Stake.CreditsMax));
		Assert.InRange(stakes.Average(), 75, 95);
	}

	[Fact]
	public void WalksOnIgnore_HitsTheStatedChance()
	{
		var walks = Enumerable.Range(0, 2_000)
			.Count(index => RollForCompulsiveStakeDef.WalksOnIgnore($"world-event-{index}"));

		Assert.InRange(walks / 2_000d, 0.45, 0.55);
	}

	[Fact]
	public void Roll_WhileTraveling_OpensTheDemand()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, deliberation: -5);
		StartJourney(run);
		var roll = TriggeringRoll(run);
		var demander = RollForCompulsiveStakeDef.SelectDemander(
			roll.RngSeed,
			WorldEventContexts.RosterPilots(run.StarSystem.Map))!;
		var stake = RollForCompulsiveStakeDef.RollStake(roll.RngSeed);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		var pending = run.StarSystem.Map.PendingWorldEvent;
		Assert.NotNull(pending);
		Assert.Equal(EWorldEventId.CompulsiveStake, pending.EventId);
		Assert.Equal(demander.Id, pending.PilotId);
		Assert.Equal(ResourceBundle.Of(ResourceId.Credits, -stake), pending.Reward);
		Assert.Equal(
			WorldEventCatalog.Stake.Announce(demander.Name, stake),
			pending.Presentation);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_ForACalculatedPilot_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, deliberation: 5);
		StartJourney(run);

		run.StarSystem.CommitSetup(new RollForCompulsiveStakeAction(StarSystemActorIds.WorldEvents, 1));
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Roll_WhileDocked_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, deliberation: -5);
		var roll = TriggeringRoll(run);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Pay_SpendsTheStakeOnceAndKeepsThePilot()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenStake(run, 80);
		var credits = Balance(run, ResourceId.Credits);
		var pilotId = pending.PilotId;

		run.StarSystem.CommitSetup(Pay(pending));
		run.StarSystem.CommitSetup(Pay(pending));

		Assert.Equal(credits - 80, Balance(run, ResourceId.Credits));
		Assert.Contains(run.PilotRegistry.All, pilot => pilot.Id == pilotId);
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Pay_WithoutTheCredits_DoesNothing()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenStake(run, 80);
		var credits = Balance(run, ResourceId.Credits);
		Assert.True(run.StarSystem.Map.PlayerResources.TryApply(
			ResourceBundle.Of(ResourceId.Credits, -credits)));

		run.StarSystem.CommitSetup(Pay(pending));

		Assert.Equal(0, Balance(run, ResourceId.Credits));
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Refuse_WhenTheRollHits_RemovesThePilotAndLeavesTheCredits()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilot = FieldOnly(run, deliberation: -5);
		var pending = OpenStake(run, 80, IdThatWalks(true));
		var credits = Balance(run, ResourceId.Credits);

		run.StarSystem.CommitSetup(Refuse(pending));
		run.StarSystem.CommitSetup(Refuse(pending));

		Assert.Equal(credits, Balance(run, ResourceId.Credits));
		Assert.DoesNotContain(run.PilotRegistry.All, candidate => candidate.Id == pilot.Id);
		Assert.DoesNotContain(pilot.AssignedShipId!, run.GetFieldedShipIds());
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Refuse_WhenTheRollMisses_KeepsThePilot()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilot = FieldOnly(run, deliberation: -5);
		var pending = OpenStake(run, 80, IdThatWalks(false));
		var credits = Balance(run, ResourceId.Credits);

		run.StarSystem.CommitSetup(Refuse(pending));

		Assert.Equal(credits, Balance(run, ResourceId.Credits));
		Assert.Contains(run.PilotRegistry.All, candidate => candidate.Id == pilot.Id);
		Assert.Contains(pilot.AssignedShipId!, run.GetFieldedShipIds());
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void SaveLoad_StillPaysOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenStake(run, 60);
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);
		var credits = Balance(restored, ResourceId.Credits);

		restored.StarSystem.CommitSetup(Pay(pending));
		restored.StarSystem.CommitSetup(Pay(pending));

		Assert.Equal(credits - 60, Balance(restored, ResourceId.Credits));
		Assert.Null(restored.StarSystem.Map.PendingWorldEvent);
	}

	private static Pilot PilotWith(int deliberation, string name = "pilot") =>
		Pilot.FromRecruitProfile(
			null,
			new PilotRecruitProfile(
				name,
				"pilot_01",
				EPilotBackgroundId.HydraulicAbuser,
				personality: new PilotPersonality(0, deliberation, 0)));

	private static Pilot FieldOnly(RunState run, int deliberation)
	{
		var ranger = RunTestKit.StarterRangerPilot(run);
		var shipId = ranger.AssignedShipId!;
		run.PilotRegistry.Unassign(ranger.Id);
		var pilot = PilotWith(deliberation, "Keel");
		run.PilotRegistry.Register(pilot);
		run.PilotRegistry.AssignShip(
			pilot.Id,
			shipId,
			run.ItemInventory,
			RunState.PlayerFleetUnitId);
		foreach (var other in run.PilotRegistry.Assigned.Where(active => active.Id != pilot.Id).ToArray())
			run.PilotRegistry.Unassign(other.Id);
		return run.PilotRegistry.Assigned.Single();
	}

	private static void StartJourney(RunState run)
	{
		var destination = run.StarSystem.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		run.StarSystem.PlayerAgent!.TryQueueMove(destination);
		run.StarSystem.AdvanceTick();
	}

	private static RollForCompulsiveStakeAction TriggeringRoll(RunState run)
	{
		var pilots = WorldEventContexts.RosterPilots(run.StarSystem.Map).ToArray();
		for (ulong seed = 0; seed < 10_000; seed++)
		{
			if (RollForCompulsiveStakeDef.SelectDemander(seed, pilots) is not null)
				return new RollForCompulsiveStakeAction(StarSystemActorIds.WorldEvents, seed);
		}

		throw new InvalidOperationException("No seed triggered a compulsive stake.");
	}

	private static string IdThatWalks(bool walks)
	{
		for (var index = 0; index < 1_000; index++)
		{
			var id = $"world-event-stake-{index}";
			if (RollForCompulsiveStakeDef.WalksOnIgnore(id) == walks)
				return id;
		}

		throw new InvalidOperationException($"No pending id {(walks ? "walks" : "stays")}.");
	}

	private static PendingWorldEvent OpenStake(RunState run, int credits, string id = "world-event-stake")
	{
		var pilot = run.PilotRegistry.Assigned[0];
		var pending = new PendingWorldEvent(
			id,
			EWorldEventId.CompulsiveStake,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			WorldEventCatalog.Stake.Announce(pilot.Name, credits),
			ResourceBundle.Of(ResourceId.Credits, -credits));
		run.StarSystem.CommitSetup(new CompulsiveStakeDemandedAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static PayCompulsiveStakeAction Pay(PendingWorldEvent pending) =>
		new(RunState.PlayerFleetUnitId, pending.Id);

	private static RefuseCompulsiveStakeAction Refuse(PendingWorldEvent pending) =>
		new(RunState.PlayerFleetUnitId, pending.Id);

	private static int Balance(RunState run, ResourceId id) =>
		run.StarSystem.Map.PlayerResources.GetBalance(id);
}
