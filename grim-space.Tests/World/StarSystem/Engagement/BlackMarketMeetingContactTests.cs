using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Contracts;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

[StarSystemTestSuite]
public sealed class BlackMarketMeetingContactTests
{
	[Fact]
	public void ReachContact_BlackMarketMeeting_OpensPlayerInput()
	{
		var (engine, _, meeting) = CreateReachedMeeting();

		Assert.True(engine.World.WaitingForPlayerInput);
		Assert.Equal(TravelTarget.None, engine.World.StateOf(RunState.PlayerFleetUnitId).TravelTarget);
		Assert.True(engine.World.FleetRegistry.Contains(meeting.MeetingId));
	}

	[Fact]
	public void LeaveContact_BlackMarketMeeting_ResumesAndKeepsMerchantUntilDeadline()
	{
		var (engine, contract, meeting) = CreateReachedMeeting();

		engine.Commit([
			new LeaveContactAction(
				RunState.PlayerFleetUnitId,
				meeting.MeetingId,
				EContactIntent.BlackMarketMeeting),
		]);

		Assert.False(engine.World.WaitingForPlayerInput);
		Assert.True(engine.World.FleetRegistry.Contains(meeting.MeetingId));
		Assert.True(engine.World.ContractRegistry.TryGetState(contract.Id, out var state));
		Assert.Equal(EContractStatus.Active, state.Status);
	}

	private static (Engine<StarMap, ActorRuntime> Engine, Contract Contract,
		BlackMarketSpaceMeetingDestination Meeting) CreateReachedMeeting()
	{
		var map = StarMap.Create(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var contract = ContractFactory.Build(
			map,
			"lead-space-contact",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 1));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		engine.Commit(ContractActionTestContext.Accept(engine.World, RunState.PlayerFleetUnitId, contract.Id));
		var meeting = (BlackMarketSpaceMeetingDestination)
			((BlackMarketSaleObjective)contract.Objective).Destination;
		new SetTravelTargetEffect(
				RunState.PlayerFleetUnitId,
				TravelTarget.Fleet(meeting.MeetingId, EContactIntent.BlackMarketMeeting))
			.Apply(engine.World, new ActorRuntime(), RunState.PlayerFleetUnitId);
		engine.Commit([
			new ReachContactAction(
				RunState.PlayerFleetUnitId,
				meeting.MeetingId,
				EContactIntent.BlackMarketMeeting),
		]);
		return (engine, contract, meeting);
	}
}
