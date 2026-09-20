using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Core.Log;
using GrimSpace.Tutorials;
using GrimSpace.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Run;

public sealed class State : IDisposable
{
	//TODO: player fleet should not be hardcoded here
	public const string PlayerFleetUnitId = "player-fleet";

	public RunShipRegistry ShipRegistry { get; } = new();
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
	private IDisposable? _fleetSpawnSubscription;
	private IDisposable? _dockyardUpgradeSubscription;
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

		ValidateOutcomeHandoffs(outcome);

		if (!StarSystem.ResolveEngagement(PlayerFleetUnitId, outcome))
		{
			PendingBattleOutcome = outcome;
			GameLog.Log("Engagement resolution failed; outcome retained.");
			return;
		}

		ApplyOutcomeToRegistry(outcome);

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
			PlayerParty.ShipIds,
			nextSeed));
	}

	public static State CreateNewRun(int seed = 0)
	{
		var run = new State();
		var playerShipId = TypedIdGenerator.NextId(UnitTypeSlug.For(BattleUnitType.Fighter));
		run.ShipRegistry.Register(ShipInstance.FromCatalog(playerShipId, BattleUnitType.Fighter));
		run.PlayerParty.Add(playerShipId);
		var orchestrator = StarSystemOrchestrator.CreateSession(
			PlayerFleetUnitId,
			run.PlayerParty.ShipIds,
			seed);
		run.BindStarSystem(orchestrator);
		return run;
	}

	public void Dispose()
	{
		_engagementSubscription?.Dispose();
		_engagementSubscription = null;
		_fleetSpawnSubscription?.Dispose();
		_fleetSpawnSubscription = null;
		_dockyardUpgradeSubscription?.Dispose();
		_dockyardUpgradeSubscription = null;
		ReleaseBattleOutcomeSubscription();
		Transitions.Dispose();
		StarSystem?.Dispose();
	}

	private void BindStarSystem(StarSystemOrchestrator orchestrator)
	{
		StarSystem = orchestrator;
		Transitions.Bind(StarSystem);
		_engagementSubscription = StarSystem.Subscribe<Record<EngagementCommitted>>(OnCommittedEngagement);
		_fleetSpawnSubscription = StarSystem.Subscribe<Record<FleetSpawned>>(OnCommittedFleetSpawned);
		_dockyardUpgradeSubscription =
			StarSystem.Subscribe<Record<DockyardUpgradePurchased>>(OnDockyardUpgradePurchased);
	}

	private void ReplaceStarSystem(StarSystemOrchestrator orchestrator)
	{
		_engagementSubscription?.Dispose();
		_engagementSubscription = null;
		_fleetSpawnSubscription?.Dispose();
		_fleetSpawnSubscription = null;
		_dockyardUpgradeSubscription?.Dispose();
		_dockyardUpgradeSubscription = null;
		StarSystem.Dispose();
		BindStarSystem(orchestrator);
	}

	internal void ReceiveEngagementFact(Record<EngagementCommitted> record) =>
		OnCommittedEngagement(record);

	private void EnsureRegistryForFleets(IEnumerable<Fleet> fleets)
	{
		foreach (var fleet in fleets)
		{
			foreach (var declaration in fleet.Registrations)
				ShipRegistry.Register(declaration);
		}

		foreach (var fleet in fleets)
		{
			foreach (var member in fleet.Members)
			{
				if (ShipRegistry.TryGet(member.Id, out _))
					continue;

				throw new InvalidOperationException(
					$"Ship '{member.Id}' on fleet '{fleet.State.Id}' is missing from RunShipRegistry.");
			}
		}
	}

	private void OnCommittedFleetSpawned(Record<FleetSpawned> record)
	{
		foreach (var declaration in record.Value.Members)
			ShipRegistry.Register(declaration);
	}

	private void OnDockyardUpgradePurchased(Record<DockyardUpgradePurchased> record)
	{
		var purchase = record.Value;
		if (!PlayerParty.ShipIds.Contains(purchase.ShipId, StringComparer.Ordinal))
		{
			GameLog.Log(
				$"Ignoring dockyard upgrade for ship '{purchase.ShipId}'; ship is not in the player party.");
			return;
		}

		if (!ShipRegistry.TryGet(purchase.ShipId, out var current))
		{
			GameLog.Log(
				$"Ignoring dockyard upgrade for ship '{purchase.ShipId}'; ship is missing from the registry.");
			return;
		}

		if (!RegistryMatchesDockyardBefore(current, purchase.Before))
		{
			GameLog.Log(
				$"Ignoring dockyard upgrade for ship '{purchase.ShipId}'; registry no longer matches purchase snapshot.");
			return;
		}

		ShipRegistry.Update(purchase.After.Clone());
	}

	private static bool RegistryMatchesDockyardBefore(ShipInstance current, ShipInstance before) =>
		string.Equals(current.Id, before.Id, StringComparison.Ordinal)
		&& current.HullPoints == before.HullPoints
		&& current.ShieldPoints.Matches(before.ShieldPoints)
		&& LoadoutMatches(current.Spec, before.Spec);

	private static bool LoadoutMatches(ShipSpec current, ShipSpec before) =>
		current.Chassis == before.Chassis
		&& current.MaxHullPoints == before.MaxHullPoints
		&& current.ShieldUpgradeTier == before.ShieldUpgradeTier
		&& current.MaxShieldPoints.Matches(before.MaxShieldPoints)
		&& current.InstalledAbilities.SequenceEqual(before.InstalledAbilities)
		&& Equals(current.TorpedoBody, before.TorpedoBody);

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
			EnsureRegistryForFleets(fleets);
			var seed = Random.Shared.Next();
			ActiveBattle = EngagementBattleFactory.Create(fleets, ShipRegistry, seed, fact.EngagementId);
			_launchedEngagementIds.Add(fact.EngagementId);
			BattleReady?.Invoke();
		}
		catch (Exception ex)
		{
			GameLog.LogException(ex, "Failed to construct battle from committed engagement.");
		}
	}

	private void ValidateOutcomeHandoffs(BattleOutcome outcome)
	{
		if (ActiveBattle is null)
			return;

		var engaged = ActiveBattle.Spawns.Select(spawn => spawn.Ship.Id).ToHashSet(StringComparer.Ordinal);
		foreach (var handoff in outcome.StateHandoffs)
		{
			if (!engaged.Contains(handoff.Id))
				throw new InvalidOperationException(
					$"Battle outcome includes unknown engaged ship '{handoff.Id}'.");

			if (!ShipRegistry.TryGet(handoff.Id, out var ship))
				throw new InvalidOperationException(
					$"Battle outcome handoff '{handoff.Id}' has no registry row.");

			if (ship.Spec.Chassis != handoff.Chassis)
				throw new InvalidOperationException(
					$"Battle outcome chassis mismatch for ship '{handoff.Id}'.");
		}
	}

	private void ApplyOutcomeToRegistry(BattleOutcome outcome)
	{
		foreach (var handoff in outcome.StateHandoffs)
			ShipRegistry.ApplyHandoff(handoff);

		foreach (var handoff in outcome.StateHandoffs)
		{
			if (handoff.HullPoints > 0)
				continue;

			PlayerParty.Remove(handoff.Id);
		}
	}

	private void ReleaseBattleOutcomeSubscription()
	{
		_battleOutcomeSubscription?.Dispose();
		_battleOutcomeSubscription = null;
	}
}
