using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;

namespace GrimSpace.Run;

public sealed class State : IDisposable
{
	//TODO: player fleet should not be hardcoded here
	public const string PlayerFleetUnitId = "player-fleet";

	public Party PlayerParty { get; } = new();
	public TutorialProgress TutorialProgress { get; } = new();
	public RunTransitionInbox Transitions { get; } = new();
	public StarSystemOrchestrator StarSystem { get; private set; } = null!;
	public BattleEncounter? ActiveBattle { get; internal set; }
	public BattleOutcome? PendingBattleOutcome { get; private set; }

	public event Action? BattleReady;

	private readonly HashSet<string> _resolvedBattleIds = new(StringComparer.Ordinal);
	private readonly HashSet<string> _launchedEngagementIds = new(StringComparer.Ordinal);
	private IDisposable? _engagementSubscription;
	private IDisposable? _battleOutcomeSubscription;

	public void OnCommittedBattleOutcome(Record<BattleOutcome> record)
	{
		var outcome = record.Value;
		if (outcome.Result == EBattleResult.Ongoing)
			return;

		if (ActiveBattle is null)
			return;

		if (!string.Equals(outcome.BattleId, ActiveBattle.Id, StringComparison.Ordinal))
		{
			GameLog.Log(
				$"Ignoring battle outcome '{outcome.BattleId}'; active engagement is '{ActiveBattle.Id}'.");
			return;
		}

		if (_resolvedBattleIds.Contains(outcome.BattleId))
			return;

		if (!StarSystem.ResolveEngagement(PlayerFleetUnitId, outcome))
		{
			PendingBattleOutcome = outcome;
			GameLog.Log("Engagement resolution failed; outcome retained.");
			return;
		}

		_resolvedBattleIds.Add(outcome.BattleId);
		PendingBattleOutcome = null;
		ActiveBattle = null;
		ReleaseBattleOutcomeSubscription();
	}

	public BattleOrchestrator CreateActiveBattleOrchestrator()
	{
		if (ActiveBattle is null)
			throw new InvalidOperationException("No active strategic battle.");

		ReleaseBattleOutcomeSubscription();
		var orchestrator = BattleOrchestrator.FromEncounter(ActiveBattle);
		_battleOutcomeSubscription = orchestrator.Subscribe<Record<BattleOutcome>>(OnCommittedBattleOutcome);
		return orchestrator;
	}

	public void RegenerateMap(int? seed = null)
	{
		var nextSeed = seed ?? Random.Shared.Next();
		ReleaseBattleOutcomeSubscription();
		ActiveBattle = null;
		_launchedEngagementIds.Clear();
		ReplaceStarSystem(StarSystemOrchestrator.CreateSession(
			PlayerFleetUnitId,
			PlayerParty.Members,
			nextSeed));
	}

	public static State CreateNewRun(int seed = 0)
	{
		var run = new State();
		var orchestrator = StarSystemOrchestrator.CreateSession(PlayerFleetUnitId, seed);
		var playerFleet = orchestrator.Map.FleetRegistry.FleetOf(PlayerFleetUnitId);
		foreach (var member in playerFleet.Members)
			run.PlayerParty.Add(member);
		run.BindStarSystem(orchestrator);
		return run;
	}

	public void Dispose()
	{
		_engagementSubscription?.Dispose();
		_engagementSubscription = null;
		ReleaseBattleOutcomeSubscription();
		Transitions.Dispose();
		StarSystem?.Dispose();
	}

	private void BindStarSystem(StarSystemOrchestrator orchestrator)
	{
		StarSystem = orchestrator;
		Transitions.Bind(StarSystem);
		_engagementSubscription = StarSystem.Subscribe<Record<EngagementCommitted>>(OnCommittedEngagement);
	}

	private void ReplaceStarSystem(StarSystemOrchestrator orchestrator)
	{
		_engagementSubscription?.Dispose();
		_engagementSubscription = null;
		StarSystem.Dispose();
		BindStarSystem(orchestrator);
	}

	internal void ReceiveEngagementFact(Record<EngagementCommitted> record) =>
		OnCommittedEngagement(record);

	private void OnCommittedEngagement(Record<EngagementCommitted> record)
	{
		var fact = record.Value;
		if (!fact.ParticipantFleetIds.Contains(PlayerFleetUnitId, StringComparer.Ordinal))
			return;

		if (_launchedEngagementIds.Contains(fact.EngagementId))
			return;

		if (ActiveBattle is not null)
			return;

		try
		{
			var fleets = fact.ParticipantFleetIds
				.Select(id => StarSystem.Map.FleetRegistry.FleetOf(id))
				.ToArray();
			var seed = Random.Shared.Next();
			ActiveBattle = EngagementBattleFactory.Create(fleets, seed, fact.EngagementId);
			_launchedEngagementIds.Add(fact.EngagementId);
			BattleReady?.Invoke();
		}
		catch (Exception ex)
		{
			GameLog.LogException(ex, "Failed to construct battle from committed engagement.");
		}
	}

	private void ReleaseBattleOutcomeSubscription()
	{
		_battleOutcomeSubscription?.Dispose();
		_battleOutcomeSubscription = null;
	}
}
