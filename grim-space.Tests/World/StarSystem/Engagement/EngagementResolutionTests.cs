using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class EngagementResolutionTests(StarMapFixture maps)
{
	private const string PlayerId = RunState.PlayerFleetUnitId;
	private const string PirateId = "pirate-a";

	[Fact]
	public void Victory_RemovesEnemyPreservesPlayerAndAllowsMovement()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var playerPosition = map.StateOf(PlayerId).CommittedPosition(map, null, 0).Position;

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(map, PirateId)));

		Assert.True(map.FleetRegistry.Contains(PlayerId));
		Assert.False(map.FleetRegistry.Contains(PirateId));
		Assert.Equal(EEngagementPhase.None, EngagementAssertions.Phase(map.StateOf(PlayerId)));
		Assert.Empty(EngagementAssertions.Participants(map.StateOf(PlayerId)));
		Assert.Equal(playerPosition, map.StateOf(PlayerId).CommittedPosition(map, null, 0).Position);

		var destination = playerPosition + new Coord(1, 0, 1);
		var path = TransitPath.FromPoints([playerPosition, destination], [1.0, 1.0]);
		Assert.True(orchestrator.CreateSimulation().TryEnqueue(
			new MoveAction(PlayerId, PlayerId, destination, path)));
	}

	[Fact]
	public void Victory_CancelsDefeatedFleetScheduledActions()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var pirate = map.StateOf(PirateId);
		var origin = pirate.IdleCoord;
		var destination = origin + new Coord(8, 0, 8);
		var runtime = orchestrator.RuntimeFor(PirateId);
		runtime.CachedPath = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		pirate.StartJourney(1, origin, destination, map.Timeline.Clock.Current);
		var completion = new CompleteMoveAction(PirateId, PirateId, 1);
		var completionTick = map.Timeline.Clock.Current + 2;
		map.Timeline.Schedule(2, completion);
		runtime.TrackPendingCompletion(completion, completionTick);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(map, PirateId)));

		Assert.Null(runtime.PendingCompletion);
		Assert.Null(runtime.CachedPath);
		Assert.False(map.Timeline.ContainsPending(action => action.ActorId == PirateId));
		orchestrator.AdvanceTicks(2);
		Assert.DoesNotContain(
			map.Timeline.History(completionTick),
			entry => entry is CompleteMoveAction action && action.ActorId == PirateId);
	}

	[Fact]
	public void Victory_CompletesContractWhenLastBoundTargetIsDestroyed()
	{
		using var orchestrator = CreateEngagement();
		var contractId = ActivateHuntContract(orchestrator.Map, [PirateId]);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(orchestrator.Map, PirateId)));

		Assert.True(orchestrator.Map.ContractRegistry.IsCompleted(contractId));
		Assert.Contains(orchestrator.Map.ContractRegistry.All, contract => contract.Id == contractId);
	}

	[Fact]
	public void Victory_DoesNotCompleteContractWhileAnotherBoundTargetSurvives()
	{
		using var orchestrator = CreateEngagement(additionalPirateId: "pirate-b");
		var contractId = ActivateHuntContract(orchestrator.Map, [PirateId, "pirate-b"]);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(orchestrator.Map, PirateId)));

		Assert.False(orchestrator.Map.ContractRegistry.IsCompleted(contractId));
		Assert.True(orchestrator.Map.ContractRegistry.TryGetActive(PlayerId, out _));
	}

	[Fact]
	public void Victory_DoesNotCompleteContractForUnrelatedFleet()
	{
		using var orchestrator = CreateEngagement(additionalPirateId: "pirate-b");
		var contractId = ActivateHuntContract(orchestrator.Map, ["pirate-b"]);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(orchestrator.Map, PirateId)));

		Assert.False(orchestrator.Map.ContractRegistry.IsCompleted(contractId));
		Assert.True(orchestrator.Map.ContractRegistry.TryGetActive(PlayerId, out _));
	}

	[Fact]
	public void RunResolution_ClearsActiveBattleOnlyAfterSuccessfulVictory()
	{
		var run = RunState.CreateNewRun(42);
		AddPirate(run.StarSystem.Map, PirateId);
		new CommitEngagementEffect(PlayerId, PirateId)
			.Apply(
				run.StarSystem.Map,
				new GrimSpace.World.StarSystem.Runtime.ActorRuntime(),
				PlayerId);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(PlayerId);
		var pirateFleet = run.StarSystem.Map.FleetRegistry.FleetOf(PirateId);
		foreach (var declaration in pirateFleet.Registrations)
			run.ShipRegistry.Register(declaration);
		var engagementId = run.StarSystem.Map.StateOf(PlayerId).CurrentEngagement!.Id;
		run.ActiveBattle = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ShipRegistry,
			1,
			engagementId);
		var activeBattle = run.ActiveBattle;
		var ongoing = new BattleOutcome(engagementId, EBattleResult.Ongoing, []);

		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(ongoing));
		Assert.Same(activeBattle, run.ActiveBattle);
		Assert.True(run.StarSystem.Map.FleetRegistry.Contains(PirateId));

		var victory = Victory(run.StarSystem.Map, PirateId);
		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(victory));
		Assert.Null(run.ActiveBattle);
		var historyCount = run.StarSystem.Map.Timeline.History().Count;

		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(victory));
		Assert.Equal(historyCount, run.StarSystem.Map.Timeline.History().Count);
	}

	[Fact]
	public void Victory_AppliesLootFromDestroyedPatrols()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var initialScrap = map.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, VictoryWithDestroyedPatrols(orchestrator.Map, PirateId)));

		var history = map.Timeline.History();
		Assert.Contains(
			history,
			entry => entry is ResolveEngagementAction { LootRolls.Count: 3 });
		Assert.Contains(
			history,
			entry => entry is Record<Transaction> { Value.Source: TransactionSource.BattleLoot });
		Assert.InRange(
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy) - initialScrap,
			150,
			360);
	}

	[Fact]
	public void Victory_BuffersResourceTransactionUntilConsumed()
	{
		using var orchestrator = CreateEngagement();
		using var inbox = new RunTransitionInbox();
		inbox.Bind(orchestrator);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, VictoryWithDestroyedPatrols(orchestrator.Map, PirateId)));

		var transaction = Assert.Single(inbox.DrainResourceTransactions());
		Assert.Equal(TransactionSource.BattleLoot, transaction.Source);
		Assert.Empty(inbox.DrainResourceTransactions());
	}

	[Fact]
	public void Victory_GrantsLootOncePerResolution()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var victory = VictoryWithDestroyedPatrols(orchestrator.Map, PirateId);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, victory));
		var scrapAfterFirst = map.PlayerResources.GetBalance(ResourceId.ScrapAlloy);
		var historyCount = map.Timeline.History().Count;

		Assert.False(orchestrator.ResolveEngagement(PlayerId, victory));
		Assert.Equal(scrapAfterFirst, map.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
		Assert.Equal(historyCount, map.Timeline.History().Count);
	}

	[Fact]
	public void Victory_PartialHuntGrantsLootBeforeContractCompletes()
	{
		using var orchestrator = CreateEngagement(additionalPirateId: "pirate-b");
		var map = orchestrator.Map;
		var contractId = ActivateHuntContract(map, [PirateId, "pirate-b"]);
		var initialScrap = map.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, VictoryWithDestroyedPatrols(orchestrator.Map, PirateId)));

		Assert.InRange(
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy) - initialScrap,
			150,
			360);
		Assert.False(map.ContractRegistry.IsCompleted(contractId));
		Assert.True(map.ContractRegistry.TryGetActive(PlayerId, out _));
	}

	[Fact]
	public void Victory_LastHuntTargetGrantsLootAndContractCredits()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var contractId = ActivateHuntContract(map, [PirateId]);
		var initialScrap = map.PlayerResources.GetBalance(ResourceId.ScrapAlloy);
		var initialCredits = map.PlayerResources.GetBalance(ResourceId.Credits);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, VictoryWithDestroyedPatrols(orchestrator.Map, PirateId)));

		Assert.InRange(
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy) - initialScrap,
			150,
			360);
		Assert.Equal(
			initialCredits + StarMap.StarterContractRewardCredits,
			map.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.True(map.ContractRegistry.IsCompleted(contractId));
	}

	[Fact]
	public void Defeat_ResolvesStrategicEngagementAndRemovesPlayer()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var defeat = new BattleOutcome(
			map.StateOf(PlayerId).CurrentEngagement!.Id,
			EBattleResult.Lose,
			[..map.FleetRegistry.All.SelectMany(fleet =>
				fleet.Members.Select(member => OutcomeTestKit.Handoff(
					member.Id,
					OutcomeTestKit.ChassisFromShipId(member.Id),
					fleet.State.Id == PlayerId ? 0 : 1)))]);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, defeat));
		Assert.False(map.FleetRegistry.Contains(PlayerId));
		Assert.True(map.FleetRegistry.Contains(PirateId));
		Assert.Equal(EEngagementPhase.None, EngagementAssertions.Phase(map.StateOf(PirateId)));
	}

	private StarSystemOrchestrator CreateEngagement(string? additionalPirateId = null)
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, PlayerId);
		AddPirate(map, PirateId);
		if (additionalPirateId is not null)
			AddPirate(map, additionalPirateId);
		new CommitEngagementEffect(PlayerId, PirateId)
			.Apply(map, new GrimSpace.World.StarSystem.Runtime.ActorRuntime(), PlayerId);
		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, PlayerId, 42, map: map);
	}

	private static void AddPirate(StarMap map, string pirateId) =>
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			EFaction.Pirates,
			new CombatProfile(EDangerLevel.VeryLow, 1)));

	private static string ActivateHuntContract(StarMap map, IReadOnlyList<string> targetIds)
	{
		var contract = map.ContractRegistry.Offered.First();
		var groupId = ((HuntObjective)contract.Objective).SpawnGroups[0].GroupId;
		map.ContractRegistry.Activate(new ContractState(
			contract.Id,
			EContractStatus.Active,
			map.Timeline.Clock.Current,
			PlayerId,
			new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
			{
				[groupId] = targetIds,
			}));
		return contract.Id;
	}

	private static BattleOutcome Victory(StarMap map, string pirateId) =>
		new(
			map.StateOf(PlayerId).CurrentEngagement!.Id,
			EBattleResult.Win,
			[..map.FleetRegistry.All.SelectMany(fleet =>
				fleet.Members.Select(member => OutcomeTestKit.Handoff(
					member.Id,
					OutcomeTestKit.ChassisFromShipId(member.Id),
					fleet.State.Id == pirateId ? 0 : 1)))]);

	private static BattleOutcome VictoryWithDestroyedPatrols(StarMap map, string pirateId) =>
		Victory(map, pirateId);
}
