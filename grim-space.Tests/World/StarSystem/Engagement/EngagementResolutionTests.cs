using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Tutorials;
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

[StarSystemTestSuite]
public sealed class EngagementResolutionTests(StarMapFixture maps)
{
	private const string PlayerId = RunState.PlayerFleetUnitId;
	private const string PirateId = "pirate-a";

	[Fact]
	public void Victory_RemovesEnemyPreservesPlayerAndAllowsMovement()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var enemyId = HuntTargetId(map, map.ContractRegistry.Pending.First().Id, 0);
		var playerPosition = map.StateOf(PlayerId).CommittedPosition(map, null, 0).Position;

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(map, enemyId)));

		Assert.True(map.FleetRegistry.Contains(PlayerId));
		Assert.False(map.FleetRegistry.Contains(enemyId));
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
		var enemyId = HuntTargetId(map, map.ContractRegistry.Pending.First().Id, 0);
		var pirate = map.StateOf(enemyId);
		var origin = pirate.IdleCoord;
		var destination = origin + new Coord(8, 0, 8);
		var runtime = orchestrator.RuntimeFor(enemyId);
		runtime.CachedPath = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		pirate.StartJourney(1, origin, destination, map.Timeline.Clock.Current);
		var completion = new CompleteMoveAction(enemyId, enemyId, 1);
		var completionTick = map.Timeline.Clock.Current + 2;
		map.Timeline.Schedule(2, completion);
		runtime.TrackPendingCompletion(completion, completionTick);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(map, enemyId)));

		Assert.Null(runtime.PendingCompletion);
		Assert.Null(runtime.CachedPath);
		Assert.False(map.Timeline.ContainsPending(action => action.ActorId == enemyId));
		orchestrator.AdvanceTicks(2);
		Assert.DoesNotContain(
			map.Timeline.History(completionTick),
			entry => entry is CompleteMoveAction action && action.ActorId == enemyId);
	}

	[Fact]
	public void Victory_CompletesContractWhenLastBoundTargetIsDestroyed()
	{
		using var orchestrator = CreateEngagement();
		var contractId = ActivateHuntContract(orchestrator.Map, [0]);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(orchestrator.Map, HuntTargetId(orchestrator.Map, contractId, 0))));

		Assert.True(orchestrator.Map.ContractRegistry.IsCompleted(contractId));
		Assert.Contains(orchestrator.Map.ContractRegistry.All, contract => contract.Id == contractId);
	}

	[Fact]
	public void Victory_DoesNotCompleteContractWhileAnotherBoundTargetSurvives()
	{
		using var orchestrator = CreateEngagement(spawnSecondHuntTarget: true);
		var contractId = ActivateHuntContract(orchestrator.Map, [0, 1]);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(orchestrator.Map, HuntTargetId(orchestrator.Map, contractId, 0))));

		Assert.False(orchestrator.Map.ContractRegistry.IsCompleted(contractId));
		Assert.True(orchestrator.Map.ContractRegistry.TryGetActive(PlayerId, out _));
	}

	[Fact]
	public void Victory_DoesNotCompleteContractForUnrelatedFleet()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var contractId = map.ContractRegistry.Pending.First().Id;
		var huntTargetId = HuntTargetId(map, contractId, 0);
		AddPirate(map, huntTargetId);
		AddPirate(map, PirateId);
		new CommitEngagementEffect(PlayerId, PirateId)
			.Apply(map, new GrimSpace.World.StarSystem.Runtime.ActorRuntime(), PlayerId);
		ActivateHuntContract(map, [0]);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, Victory(map, PirateId)));

		Assert.False(map.ContractRegistry.IsCompleted(contractId));
		Assert.True(map.ContractRegistry.TryGetActive(PlayerId, out _));
	}

	[Fact]
	public void RunResolution_CompletesBeatAAndAddsBeatBStoryObjective()
	{
		using var run = RunState.CreateNewRun(42, tutorialsEnabled: true);
		var beatAId = Assert.IsType<string>(run.TutorialState?.BeatAContractId);
		var huntTargetId = HuntTargetId(run.StarSystem.Map, beatAId, 0);
		AddPirate(run.StarSystem.Map, huntTargetId);
		new CommitEngagementEffect(PlayerId, huntTargetId)
			.Apply(
				run.StarSystem.Map,
				new GrimSpace.World.StarSystem.Runtime.ActorRuntime(),
				PlayerId);
		ActivateHuntContract(run.StarSystem.Map, [0]);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(PlayerId);
		var pirateFleet = run.StarSystem.Map.FleetRegistry.FleetOf(huntTargetId);
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
		Assert.True(run.StarSystem.Map.FleetRegistry.Contains(huntTargetId));

		var victory = Victory(run.StarSystem.Map, huntTargetId);
		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(victory));
		Assert.Null(run.ActiveBattle);
		Assert.True(run.StarSystem.Map.ContractRegistry.IsCompleted(beatAId));
		var beatBId = Assert.IsType<string>(run.TutorialState?.BeatBContractId);
		Assert.True(run.StarSystem.Map.ContractRegistry.IsPending(beatBId));
		Assert.Contains(
			run.StarSystem.Map.StoryObjectives.Active,
			objective => objective.RequiredContractId == beatBId);
		var historyCount = run.StarSystem.Map.Timeline.History().Count;

		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(victory));
		Assert.Equal(historyCount, run.StarSystem.Map.Timeline.History().Count);
	}

	[Fact]
	public void Victory_AppliesLootFromDestroyedPatrols()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var enemyId = HuntTargetId(map, map.ContractRegistry.Pending.First().Id, 0);
		var initialScrap = map.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, VictoryWithDestroyedPatrols(map, enemyId)));

		var history = map.Timeline.History();
		Assert.Contains(
			history,
			entry => entry is ResolveEngagementAction { LootRolls.Count: 1 });
		Assert.Contains(
			history,
			entry => entry is Record<Transaction> { Value.Source: TransactionSource.BattleLoot });
		Assert.InRange(
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy) - initialScrap,
			50,
			120);
	}

	[Fact]
	public void Victory_BuffersResourceTransactionUntilConsumed()
	{
		using var orchestrator = CreateEngagement();
		using var inbox = new RunTransitionInbox();
		inbox.Bind(orchestrator);
		var enemyId = HuntTargetId(orchestrator.Map, orchestrator.Map.ContractRegistry.Pending.First().Id, 0);

		Assert.True(orchestrator.ResolveEngagement(PlayerId, VictoryWithDestroyedPatrols(orchestrator.Map, enemyId)));

		var transaction = Assert.Single(inbox.DrainResourceTransactions());
		Assert.Equal(TransactionSource.BattleLoot, transaction.Source);
		Assert.Empty(inbox.DrainResourceTransactions());
	}

	[Fact]
	public void Victory_GrantsLootOncePerResolution()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var enemyId = HuntTargetId(orchestrator.Map, orchestrator.Map.ContractRegistry.Pending.First().Id, 0);
		var victory = VictoryWithDestroyedPatrols(orchestrator.Map, enemyId);

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
		using var orchestrator = CreateEngagement(spawnSecondHuntTarget: true);
		var map = orchestrator.Map;
		var contractId = ActivateHuntContract(map, [0, 1]);
		var initialScrap = map.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		Assert.True(orchestrator.ResolveEngagement(
			PlayerId,
			VictoryWithDestroyedPatrols(map, HuntTargetId(map, contractId, 0))));

		Assert.InRange(
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy) - initialScrap,
			50,
			120);
		Assert.False(map.ContractRegistry.IsCompleted(contractId));
		Assert.True(map.ContractRegistry.TryGetActive(PlayerId, out _));
	}

	[Fact]
	public void Victory_LastHuntTargetGrantsLootAndContractCredits()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var contractId = map.ContractRegistry.Pending.First().Id;
		ActivateHuntContract(map, [0]);
		var initialScrap = map.PlayerResources.GetBalance(ResourceId.ScrapAlloy);
		var initialCredits = map.PlayerResources.GetBalance(ResourceId.Credits);

		Assert.True(orchestrator.ResolveEngagement(
			PlayerId,
			VictoryWithDestroyedPatrols(map, HuntTargetId(map, contractId, 0))));

		Assert.InRange(
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy) - initialScrap,
			50,
			120);
		Assert.Equal(
			initialCredits + TutorialBeatContracts.BeatAHuntRewardCredits,
			map.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.True(map.ContractRegistry.IsCompleted(contractId));
	}

	[Fact]
	public void Defeat_ResolvesStrategicEngagementAndRemovesPlayer()
	{
		using var orchestrator = CreateEngagement();
		var map = orchestrator.Map;
		var enemyId = HuntTargetId(map, map.ContractRegistry.Pending.First().Id, 0);
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
		Assert.True(map.FleetRegistry.Contains(enemyId));
		Assert.Equal(EEngagementPhase.None, EngagementAssertions.Phase(map.StateOf(enemyId)));
	}

	private StarSystemOrchestrator CreateEngagement(
		string? additionalPirateId = null,
		bool spawnSecondHuntTarget = false)
	{
		var map = maps.FreshWithBeatAHunt(42);
		StarSystemTestHarness.AddPlayerFleet(map, PlayerId);
		var contractId = map.ContractRegistry.Pending.First().Id;
		var primaryTargetId = HuntTargetId(map, contractId, 0);
		AddPirate(map, primaryTargetId);
		if (spawnSecondHuntTarget)
			AddPirate(map, HuntTargetId(map, contractId, 1));
		if (additionalPirateId is not null)
			AddPirate(map, additionalPirateId);
		new CommitEngagementEffect(PlayerId, primaryTargetId)
			.Apply(map, new GrimSpace.World.StarSystem.Runtime.ActorRuntime(), PlayerId);
		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, PlayerId, 42, map: map);
	}

	private static void AddPirate(StarMap map, string pirateId) =>
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			EFaction.Pirates,
			new CombatProfile(EDangerLevel.VeryLow, 1)));

	private static string HuntTargetId(StarMap map, string contractId, int index)
	{
		var hunt = (HuntObjective)map.ContractRegistry.All.First(contract => contract.Id == contractId).Objective;
		var group = hunt.SpawnGroups[0];
		return $"{contractId}.{group.GroupId}.{index}";
	}

	private static string ActivateHuntContract(StarMap map, IReadOnlyList<int> boundTargetIndices)
	{
		var contract = map.ContractRegistry.Pending.First();
		var hunt = (HuntObjective)contract.Objective;
		var group = hunt.SpawnGroups[0];
		var requiredCount = boundTargetIndices.Count == 0
			? group.RequiredCount
			: boundTargetIndices.Max() + 1;
		if (requiredCount > group.RequiredCount)
		{
			map.ContractRegistry.Remove(contract.Id);
			var expandedGroup = group with { RequiredCount = requiredCount };
			contract = contract with { Objective = new HuntObjective([expandedGroup]) };
			Assert.True(map.ContractRegistry.TryAdd(contract));
		}

		foreach (var index in boundTargetIndices)
		{
			var derivedId = HuntTargetId(map, contract.Id, index);
			Assert.True(map.FleetRegistry.Contains(derivedId));
		}

		map.ContractRegistry.Activate(new ContractState(
			contract.Id,
			EContractStatus.Active,
			map.Timeline.Clock.Current,
			PlayerId));
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
