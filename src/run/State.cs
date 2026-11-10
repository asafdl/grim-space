using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Core.Log;
using GrimSpace.Pilots;
using GrimSpace.Pilots.Barks;
using GrimSpace.Tutorials;

using GrimSpace.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.PilotCapabilities;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Run.Items;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Run;

public sealed class State : IDisposable
{
	public const string PlayerFleetUnitId = StarSystemActorIds.PlayerFleet;
	public Inventory ItemInventory { get; } = new();
	public RunPilotRegistry PilotRegistry { get; } = new();
	private TutorialState? _tutorialState;

	public TutorialState? TutorialState => _tutorialState;

	public TutorialController? Tutorials { get; private set; }

	public bool PendingTutorialGraduation =>
		_tutorialState?.PendingTutorialGraduation ?? false;
	public RunTransitionInbox Transitions { get; } = new();
	public StarSystemOrchestrator StarSystem { get; private set; } = null!;
	private PilotBarkDirector? _barks;
	public PilotBarkDirector Barks => _barks ??= new PilotBarkDirector(this, new Random());
	public BattleEncounter? ActiveBattle { get; internal set; }
	public BattleOutcome? PendingBattleOutcome { get; private set; }
	public BattleWorld? RestoredBattleWorld { get; private set; }

	/// <summary>True when the pilot roster is empty. Independent of battle objective result.</summary>
	public bool IsRunOver { get; private set; }

	private bool _openingPending;

	/// <summary>True once for a freshly created run; not persisted, so loaded saves never report it.</summary>
	public bool TryConsumeOpening()
	{
		var pending = _openingPending;
		_openingPending = false;
		return pending;
	}

	public RunStatistics Statistics { get; } = new();

	public event Action? BattleReady;

	/// <summary>Rising edge only: fires when <see cref="IsRunOver"/> becomes true.</summary>
	public event Action? RunOverChanged;

	public State()
	{
		PilotRegistry.PilotRemoved += RefreshRunOver;
	}

	public RunStateSnapshotDto CaptureSnapshot(
		PersistenceRegistry registry,
		BattleWorld? battleWorld = null)
	{
		ArgumentNullException.ThrowIfNull(registry);
		return new RunStateSnapshotDto(
			ItemInventory.CaptureSnapshot()
				.Select(entry => SaveDtoMapper.CaptureShip(entry.Ship, entry.OwnerId))
				.ToArray(),
			PilotRegistry.CaptureSnapshot(),
			_tutorialState is null ? null : new TutorialStateDto(
				_tutorialState.BeatAContractId,
				_tutorialState.BeatBContractId,
				Tutorials?.ActiveFlow?.Id,
				_tutorialState.ActiveStepIndex,
				_tutorialState.PendingTutorialGraduation,
				_tutorialState.CaptureCompletedFlows()),
			SaveDtoMapper.CaptureStarMap(StarSystem.Map, registry),
			StarSystem.ContractGenerationEnabled,
			StarSystem.SimMode,
			ActiveBattle is null ? null : SaveDtoMapper.CaptureBattleEncounter(ActiveBattle),
			PendingBattleOutcome,
			_resolvedBattleIds.ToArray(),
			_launchedEngagementIds.ToArray(),
			battleWorld is null ? null : SaveDtoMapper.CaptureBattleWorld(battleWorld, registry),
			StarSystem.CaptureRuntimeSnapshots(registry),
			Statistics.CaptureSnapshot());
	}

	public static State FromSnapshot(
		RunStateSnapshotDto snapshot,
		PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(snapshot);
		ArgumentNullException.ThrowIfNull(registry);

		var run = new State();
		run.ItemInventory.RestoreSnapshot(snapshot.Ships.Select(dto =>
		{
			if (string.IsNullOrEmpty(dto.OwnerId))
				throw new InvalidDataException($"Ship '{dto.Id}' is missing OwnerId.");

			return new InventorySnapshotEntry(SaveDtoMapper.RestoreShip(dto), dto.OwnerId);
		}));
		run.PilotRegistry.RestoreSnapshot(snapshot.Pilots);
		run.ValidatePilotAssignments();

		var map = SaveDtoMapper.RestoreStarMap(
			snapshot.StarMap,
			registry,
			run.ItemInventory);

		run.BindStarSystem(StarSystemOrchestrator.FromMapForRestore(
			map,
			PlayerFleetUnitId));
		run.SyncStarPlayerPipeline();
		run.StarSystem.SetContractGenerationEnabled(snapshot.ContractGenerationEnabled);
		if (snapshot.SimMode == ESimMode.Stepped)
			run.StarSystem.SetStepped();
		else
			run.StarSystem.SetRunning();
		run.StarSystem.RestoreRuntimeSnapshots(snapshot.StarSystemRuntimes, registry);
		run.ActiveBattle = snapshot.ActiveBattle is null
			? null
			: SaveDtoMapper.RestoreBattleEncounter(snapshot.ActiveBattle);
		run.RestoredBattleWorld = snapshot.BattleWorld is null
			? null
			: SaveDtoMapper.RestoreBattleWorld(snapshot.BattleWorld, registry);
		run.PendingBattleOutcome = snapshot.PendingBattleOutcome;
		run._resolvedBattleIds.UnionWith(snapshot.ResolvedBattleIds);
		run._launchedEngagementIds.UnionWith(snapshot.LaunchedEngagementIds);
		run.Statistics.RestoreSnapshot(snapshot.Statistics);

		if (snapshot.Tutorial is { } tutorial)
		{
			run._tutorialState = new TutorialState
			{
				BeatAContractId = tutorial.BeatAContractId,
				BeatBContractId = tutorial.BeatBContractId,
				ActiveStepIndex = tutorial.ActiveStepIndex,
				PendingTutorialGraduation = tutorial.PendingTutorialGraduation,
			};
			run._tutorialState.RestoreCompletedFlows(tutorial.CompletedFlows);
			run.Tutorials = new TutorialController(run.StarSystem, run._tutorialState);
			run.Tutorials.EnsureContractObservation();
			if (tutorial.ActiveFlowId is not null)
				run.Tutorials.RestoreActiveFlow(
					tutorial.ActiveFlowId,
					tutorial.ActiveStepIndex);
			run.Tutorials.ReconcileFromWorldState(cancelBattleFlowWhenOffBattlefield: false);
			run.Tutorials.FlowCompleted += run.OnTutorialFlowCompleted;
		}

		run.SyncContractGenerationFromTutorialState();
		run.RefreshRunOver();
		return run;
	}

	public IReadOnlyList<string> GetFieldedShipIds()
	{
		return ItemInventory.ShipsOwnedBy(PlayerFleetUnitId)
			.Where(ship => PilotRegistry.TryGetForShip(ship.Id, out _))
			.Select(ship => ship.Id)
			.ToArray();
	}

	private readonly HashSet<string> _resolvedBattleIds = new(StringComparer.Ordinal);
	private readonly HashSet<string> _launchedEngagementIds = new(StringComparer.Ordinal);
	private IDisposable? _engagementSubscription;
	private IDisposable? _fleetSpawnSubscription;
	private IDisposable? _merchantShipPurchaseSubscription;
	private IDisposable? _fieldedShipDamageSubscription;
	private IDisposable? _playerShipEnlistedSubscription;
	private IDisposable? _ownedShipPurchasedSubscription;
	private IDisposable? _pilotHireCommittedSubscription;
	private IDisposable? _pilotSalaryPayrollSubscription;
	private IDisposable? _pilotWalkedOutSubscription;
	private IDisposable? _pilotHazardPayrollSubscription;
	private IDisposable? _battleOutcomeSubscription;
	private IDisposable? _reachContactSubscription;
	private IDisposable? _reachWreckSubscription;
	private IDisposable? _fleeSubscription;
	private PreparedBattle? _preparedBattle;
	private (EngagementCommitted Fact, Task<PreparedBattleBuild> Build)? _battleLaunch;
	private PreparedBattleBuild? _adoptedBattle;

	private sealed record PreparedBattleBuild(BattleEncounter Encounter, BattleOrchestrator Battle);

	private sealed record PreparedBattle(
		string EngagementId,
		string InitiatorFleetId,
		string[] ParticipantFleetIds,
		IReadOnlySet<string> Crews,
		Task<PreparedBattleBuild> Build);

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
		{
			if (ActiveBattle is not null
				&& string.Equals(ActiveBattle.Id, outcome.BattleId, StringComparison.Ordinal))
			{
				ReleaseResolvedBattle(outcome.BattleId);
				GameLog.Log(
					$"Ignored duplicate outcome for '{outcome.BattleId}' and released the active battle.");
			}

			return;
		}

		ValidateOutcomeHandoffs(outcome);

		if (!StarSystem.ResolveEngagement(PlayerFleetUnitId, outcome))
		{
			PendingBattleOutcome = outcome;
			GameLog.Log("Engagement resolution failed; outcome retained.");
			return;
		}

		GameLog.Log(
			$"Battle outcome resolved: id='{outcome.BattleId}', result='{outcome.Result}', "
			+ $"tutorialsEnabled={Tutorials is not null}.");
		if (outcome.Result == EBattleResult.Win)
			GrantPilotBattleXp(ActiveBattle.Spawns, ActiveBattle.XpDifficulty, outcome);

		Statistics.RecordBattle(outcome.Result);
		ApplyOutcomeToRegistry(outcome);

		_resolvedBattleIds.Add(outcome.BattleId);
		PendingBattleOutcome = null;
		ActiveBattle = null;
		ReleaseBattleOutcomeSubscription();
	}

	internal bool TryReturnFromBattle(string battleId)
	{
		if (ActiveBattle is null)
			return true;

		if (!string.Equals(ActiveBattle.Id, battleId, StringComparison.Ordinal))
		{
			GameLog.Log(
				$"Return to star map ignored; battle '{battleId}' is not the active engagement '{ActiveBattle.Id}'.");
			return false;
		}

		if (!_resolvedBattleIds.Contains(battleId))
		{
			GameLog.Log($"Return to star map ignored; engagement '{battleId}' is not resolved.");
			return false;
		}

		ReleaseResolvedBattle(battleId);
		GameLog.Log($"Released already-resolved engagement '{battleId}'.");
		return true;
	}

	private void ReleaseResolvedBattle(string engagementId)
	{
		foreach (var fleet in StarSystem.Map.FleetRegistry.All)
		{
			if (string.Equals(fleet.State.CurrentEngagement?.Id, engagementId, StringComparison.Ordinal))
				fleet.State.CurrentEngagement = null;
		}

		if (ActiveBattle is null
			|| !string.Equals(ActiveBattle.Id, engagementId, StringComparison.Ordinal))
			return;

		ActiveBattle = null;
		PendingBattleOutcome = null;
		ReleaseBattleOutcomeSubscription();
	}

	public RunStatisticsSummary CaptureStatisticsSummary() =>
		new(
			StarSystem.Map.ContractRegistry.CountCompleted(),
			Statistics.BattlesWon,
			Statistics.BattlesLost,
			Statistics.CreditsEarned,
			StarSystem.Map.PlayerResources.GetBalance(ResourceId.Credits));

	public BattleOrchestrator CreateActiveBattleOrchestrator()
	{
		if (ActiveBattle is null)
			throw new InvalidOperationException("No active strategic battle.");

		if (RestoredBattleWorld is { } worldToValidate)
			BattleEngagementComposition.EnsurePlayableUnits(
				worldToValidate.UnitRegistry.All,
				ActiveBattle.Id);
		else
			BattleEngagementComposition.EnsurePlayable(ActiveBattle.Spawns, ActiveBattle.Id);

		ReleaseBattleOutcomeSubscription();
		var prepared = RestoredBattleWorld is null
			&& _adoptedBattle is { } adopted
			&& ReferenceEquals(adopted.Encounter, ActiveBattle)
				? adopted.Battle
				: null;
		if (prepared is null)
			_adoptedBattle?.Battle.Dispose();
		_adoptedBattle = null;

		var orchestrator = RestoredBattleWorld is { } savedWorld
			? BattleOrchestrator.FromSavedWorld(
				savedWorld,
				ResolveActiveBattlePlayerId(savedWorld),
				ActiveBattle)
			: prepared ?? BattleOrchestrator.FromEncounter(
				ActiveBattle,
				preferredPlayerId: ResolveActiveBattlePlayerId(null));
		RestoredBattleWorld = null;
		_battleOutcomeSubscription = orchestrator.Subscribe<Record<BattleOutcome>>(OnCommittedBattleOutcome);
		prepared?.Start("encounter ready");
		return orchestrator;
	}

	internal void AbandonUnplayableActiveBattle()
	{
		ReleaseBattleOutcomeSubscription();
		RestoredBattleWorld = null;
		ActiveBattle = null;
	}

	/// <summary>Lead is the first last-deployed pilot's ship present in battle, else the first player unit.</summary>
	private string ResolveActiveBattlePlayerId(BattleWorld? savedWorld)
	{
		string[] playerUnitIds = savedWorld is not null
			? savedWorld.UnitRegistry.All
				.Where(unit => unit.Team == ETeam.Player)
				.Select(unit => unit.State.Id)
				.ToArray()
			: ActiveBattle!.Spawns
				.Where(spawn => spawn.Team == ETeam.Player)
				.Select(spawn => spawn.Ship.Id)
				.ToArray();

		return PilotRegistry.LastDeployment
			.Select(pilotId => PilotRegistry.TryGet(pilotId, out var pilot) ? pilot.AssignedShipId : null)
			.FirstOrDefault(shipId => shipId is not null && playerUnitIds.Contains(shipId, StringComparer.Ordinal))
			?? playerUnitIds.FirstOrDefault()
			?? throw new InvalidOperationException("Battle has no player units.");
	}

	public bool TryEnlistPlayerShip(ShipSpawnDeclaration declaration)
	{
		ArgumentNullException.ThrowIfNull(declaration);
		if (!CanAcquirePlayerShip(declaration))
			return false;

		var action = new EnlistPlayerShipAction(PlayerFleetUnitId, declaration);
		StarSystem.CommitSetup(action);
		return true;
	}

	public bool CanAcquirePlayerShip(ShipSpawnDeclaration declaration)
	{
		ArgumentNullException.ThrowIfNull(declaration);
		var shipId = declaration.ShipId;
		if (string.IsNullOrWhiteSpace(shipId))
			return false;
		if (ItemInventory.TryGet(shipId, out _))
			return false;
		if (PilotRegistry.TryGetForShip(shipId, out _))
			return false;
		if (ActiveBattle is not null)
			return false;
		if (!StarSystem.Map.FleetRegistry.TryGet(PlayerFleetUnitId, out var fleet))
			return false;
		if (fleet.State.CurrentEngagement?.Phase == EEngagementPhase.Engaged)
			return false;

		var action = new EnlistPlayerShipAction(PlayerFleetUnitId, declaration);
		var runtime = StarSystem.RuntimeFor(PlayerFleetUnitId);
		return EnlistPlayerShipActionDef.Instance.IsLegal(action, StarSystem.Map, runtime);
	}

	private bool CanCompletePlayerShipAcquire(ShipSpawnDeclaration declaration)
	{
		var shipId = declaration.ShipId;
		if (string.IsNullOrWhiteSpace(shipId))
			return false;
		if (ItemInventory.TryGet(shipId, out _))
			return false;
		if (PilotRegistry.TryGetForShip(shipId, out _))
			return false;

		return true;
	}

	internal bool TryAcquirePlayerShip(ShipSpawnDeclaration declaration)
	{
		ArgumentNullException.ThrowIfNull(declaration);
		if (!CanCompletePlayerShipAcquire(declaration))
			return false;

		ApplyPlayerShipAcquire(declaration);
		RefreshStrategicFleetProjection();
		return true;
	}

	public bool TryLearnPilotSkill(string pilotId, EPilotSkillId skillId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		if (!PilotRegistry.TryGet(pilotId, out var pilot))
			return false;
		if (!PilotSkillAvailability.AvailableToLearn(pilot).Contains(skillId))
			return false;

		PilotRegistry.Update(pilot.LearnPilotSkill(skillId));
		if (PilotSkillCatalog.Get(skillId).CapabilityIds.Any(capabilityId =>
			StarMapPilotCapabilityPipelineFactory.Supports(capabilityId)))
			RefreshStrategicPilotCapabilities();
		return true;
	}

	public bool TryGrantDebugMaxPilotExperience()
	{
		var any = false;
		foreach (var pilot in PilotRegistry.All.ToArray())
		{
			var updated = PilotProgression.GrantMaxExperience(pilot);
			if (ReferenceEquals(updated, pilot))
				continue;

			PilotRegistry.Update(updated);
			any = true;
			GameLog.Log(
				$"Debug pilot XP: '{updated.Name}' → Lv {PilotProgression.LevelFor(updated.ExperiencePoints)} "
				+ $"({updated.ExperiencePoints} total).");
		}

		return any;
	}

	public bool TryAssignPilotToShip(string pilotId, string shipId) =>
		TryAssignPilotToShip(pilotId, shipId, out _);

	public bool TryDismissPilot(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		if (ActiveBattle is not null)
			return false;
		if (!PilotRegistry.TryGet(pilotId, out _))
			return false;
		if (PilotRegistry.All.Count <= 1)
			return false;

		PilotRegistry.Remove(pilotId);
		RefreshPilotAssignments();
		return true;
	}

	private EPilotAssignmentFailureReason GetPilotAssignmentFailureReason(
		string pilotId,
		string shipId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		if (ActiveBattle is not null)
			return EPilotAssignmentFailureReason.BattleLocked;
		if (!PilotRegistry.TryGet(pilotId, out var pilot))
			return EPilotAssignmentFailureReason.PilotNotFound;
		if (!ItemInventory.IsOwnedBy(shipId, PlayerFleetUnitId))
			return EPilotAssignmentFailureReason.ShipNotOwned;
		if (!OperatorClassCompatibility.CanOperate(
			pilot.ClassId,
			ItemInventory.Get(shipId).Spec.ClassId))
			return EPilotAssignmentFailureReason.IncompatibleClass;
		if (!string.IsNullOrEmpty(pilot.AssignedShipId)
			&& !ItemInventory.IsOwnedBy(pilot.AssignedShipId, PlayerFleetUnitId))
			return EPilotAssignmentFailureReason.ShipNotOwned;
		if (PilotRegistry.TryGetForShip(shipId, out var occupant)
			&& occupant.Id != pilotId
			&& !string.IsNullOrEmpty(pilot.AssignedShipId)
			&& !OperatorClassCompatibility.CanOperate(
				occupant.ClassId,
				ItemInventory.Get(pilot.AssignedShipId).Spec.ClassId))
			return EPilotAssignmentFailureReason.IncompatibleClass;

		return EPilotAssignmentFailureReason.None;
	}

	public bool TryAssignPilotToShip(
		string pilotId,
		string shipId,
		out EPilotAssignmentFailureReason failureReason)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		failureReason = GetPilotAssignmentFailureReason(pilotId, shipId);
		if (failureReason != EPilotAssignmentFailureReason.None)
			return false;
		if (!PilotRegistry.TryGet(pilotId, out var pilot))
		{
			failureReason = EPilotAssignmentFailureReason.PilotNotFound;
			return false;
		}
		if (string.Equals(pilot.AssignedShipId, shipId, StringComparison.Ordinal))
			return true;

		PilotRegistry.AssignPilotToShip(
			pilotId,
			shipId,
			ItemInventory,
			PlayerFleetUnitId);
		RefreshPilotAssignments();
		return true;
	}

	/// <summary>Refreshes fleet projection and pilot capabilities after ship assignments change.</summary>
	public void RefreshPilotAssignments()
	{
		RefreshStrategicFleetProjection();
		RefreshStrategicPilotCapabilities();
	}

	public void RegenerateMap(int? seed = null)
	{
		var nextSeed = seed ?? Random.Shared.Next();
		ReleaseBattleOutcomeSubscription();
		ActiveBattle = null;
		_launchedEngagementIds.Clear();
		ReplaceStarSystem(StarSystemOrchestrator.CreateSession(
			PlayerFleetUnitId,
			GetFieldedShipIds(),
			nextSeed,
			ItemInventory));
		SyncStarPlayerPipeline();
		SyncContractGenerationFromTutorialState();
		SeedContractBoardIfGenerationEnabled();
	}

	public static State CreateNewRun(int seed = 0, bool tutorialsEnabled = false)
	{
		var run = new State();
		var rng = new Random(seed);
		BattleUnitType[] starterChassis =
		[
			BattleUnitType.Fighter,
			BattleUnitType.RepurposedMiner,
			BattleUnitType.IndustrialGooper,
			BattleUnitType.Rogue,
		];
		rng.Shuffle(starterChassis);
		for (var index = 0; index < 2; index++)
		{
			var chassis = starterChassis[index];
			var shipId = TypedIdGenerator.NextId(UnitTypeSlug.For(chassis));
			run.ApplyPlayerShipAcquire(
				new ShipSpawnDeclaration(shipId, chassis, EShipGearTier.T0),
				createStarterPilot: index == 0,
				rng);
		}
		var orchestrator = StarSystemOrchestrator.CreateSession(
			PlayerFleetUnitId,
			run.GetFieldedShipIds(),
			seed,
			run.ItemInventory);
		run.BindStarSystem(orchestrator);
		run.SyncStarPlayerPipeline();
		run.ConfigureTutorials(tutorialsEnabled);
		run.SeedContractBoardIfGenerationEnabled();
		run.RefreshRunOver();
		run._openingPending = true;
		return run;
	}

	private void SeedContractBoardIfGenerationEnabled()
	{
		if (StarSystem.ContractGenerationEnabled)
			StarSystem.SeedContractBoard();
	}

	private void RefreshRunOver()
	{
		var runOver = PilotRegistry.All.Count == 0;
		if (runOver == IsRunOver)
			return;

		IsRunOver = runOver;
		if (runOver)
			RunOverChanged?.Invoke();
	}

	public void ConfigureTutorials(bool enabled)
	{
		if (Tutorials is not null)
			Tutorials.FlowCompleted -= OnTutorialFlowCompleted;

		if (!enabled)
		{
			Tutorials?.Dispose();
			Tutorials = null;
			_tutorialState = null;
			StarSystem.SetContractGenerationEnabled(true);
			SeedContractBoardIfGenerationEnabled();
			return;
		}

		StarSystem.Map.ContractRegistry.ClearPending();
		_tutorialState = new TutorialState();
		Tutorials = new TutorialController(StarSystem, _tutorialState);
		Tutorials.InitializeBeatProgression();
		Tutorials.FlowCompleted += OnTutorialFlowCompleted;
		SyncContractGenerationFromTutorialState();
	}

	private void SyncContractGenerationFromTutorialState()
	{
		var enabled = Tutorials is null
			|| (_tutorialState?.IsFlowCompleted(TutorialController.GraduationFlowId) ?? false);
		StarSystem.SetContractGenerationEnabled(enabled);
	}

	private void OnTutorialFlowCompleted(TutorialFlow flow)
	{
		if (flow.Id != TutorialController.GraduationFlowId)
			return;

		StarSystem.SetContractGenerationEnabled(true);
		SeedContractBoardIfGenerationEnabled();
	}

	public void Dispose()
	{
		PilotRegistry.PilotRemoved -= RefreshRunOver;
		if (Tutorials is not null)
			Tutorials.FlowCompleted -= OnTutorialFlowCompleted;
		Tutorials?.Dispose();
		Tutorials = null;
		_barks?.Dispose();
		ReleaseStarSystemSubscriptions();
		ReleaseBattleOutcomeSubscription();
		if (_battleLaunch is { } launch)
			DisposeWhenBuilt(launch.Build);
		_battleLaunch = null;
		_adoptedBattle?.Battle.Dispose();
		_adoptedBattle = null;
		Transitions.Dispose();
		StarSystem?.Dispose();
	}

	private void BindStarSystem(StarSystemOrchestrator orchestrator)
	{
		StarSystem = orchestrator;
		Transitions.Bind(StarSystem);
		_engagementSubscription = StarSystem.Subscribe<Record<EngagementCommitted>>(OnCommittedEngagement);
		_fleetSpawnSubscription = StarSystem.Subscribe<Record<FleetSpawned>>(OnCommittedFleetSpawned);
		_merchantShipPurchaseSubscription =
			StarSystem.Subscribe<Record<MerchantShipPurchase>>(OnMerchantShipPurchase);
		_fieldedShipDamageSubscription =
			StarSystem.Subscribe<Record<FieldedShipDamage>>(OnFieldedShipDamage);
		_playerShipEnlistedSubscription =
			StarSystem.Subscribe<Record<PlayerShipEnlisted>>(OnPlayerShipEnlisted);
		_ownedShipPurchasedSubscription =
			StarSystem.Subscribe<Record<OwnedShipPurchased>>(OnOwnedShipPurchased);
		_pilotHireCommittedSubscription =
			StarSystem.Subscribe<Record<PilotHireCommitted>>(OnPilotHireCommitted);
		_pilotSalaryPayrollSubscription =
			StarSystem.Subscribe<Record<PilotSalaryPayroll>>(OnPilotSalaryPayroll);
		_pilotWalkedOutSubscription =
			StarSystem.Subscribe<Record<string>>(OnPilotWalkedOut);
		_pilotHazardPayrollSubscription =
			StarSystem.Subscribe<Record<HazardPayrollResult>>(OnPilotHazardPayroll);
		_reachContactSubscription = StarSystem.Subscribe<ReachContactAction>(contact =>
		{
			if (contact.Intent == EContactIntent.Engagement
				&& (contact.InitiatorId == PlayerFleetUnitId || contact.TargetId == PlayerFleetUnitId))
				PrepareBattle();
		});
		_reachWreckSubscription = StarSystem.Subscribe<ReachWreckageAction>(reach =>
		{
			if (reach.ActorId == PlayerFleetUnitId)
				PrepareBattle();
		});
		_fleeSubscription = StarSystem.Subscribe<FleeAction>(flee =>
		{
			if (flee.ActorId == PlayerFleetUnitId)
				DiscardPreparedBattle();
		});
		StarSystem.ResourceTransactionCommitted += OnResourceTransactionForStatistics;
		StarSystem.BindSalaryPayrollRoster(SnapshotSalaryPayrollRoster);
		StarSystem.BindFleetSalaryDiscountStacks(SnapshotFleetSalaryDiscountStacks);
		StarSystem.Map.PilotRegistry = PilotRegistry;
		_barks?.BindStarSystem();
	}

	private void ReplaceStarSystem(StarSystemOrchestrator orchestrator)
	{
		ReleaseStarSystemSubscriptions();
		StarSystem.Dispose();
		BindStarSystem(orchestrator);
	}

	private void ReleaseStarSystemSubscriptions()
	{
		_engagementSubscription?.Dispose();
		_engagementSubscription = null;
		_fleetSpawnSubscription?.Dispose();
		_fleetSpawnSubscription = null;
		_merchantShipPurchaseSubscription?.Dispose();
		_merchantShipPurchaseSubscription = null;
		_fieldedShipDamageSubscription?.Dispose();
		_fieldedShipDamageSubscription = null;
		_playerShipEnlistedSubscription?.Dispose();
		_playerShipEnlistedSubscription = null;
		_ownedShipPurchasedSubscription?.Dispose();
		_ownedShipPurchasedSubscription = null;
		_pilotHireCommittedSubscription?.Dispose();
		_pilotHireCommittedSubscription = null;
		_pilotSalaryPayrollSubscription?.Dispose();
		_pilotSalaryPayrollSubscription = null;
		_pilotWalkedOutSubscription?.Dispose();
		_pilotWalkedOutSubscription = null;
		_pilotHazardPayrollSubscription?.Dispose();
		_pilotHazardPayrollSubscription = null;
		_reachContactSubscription?.Dispose();
		_reachContactSubscription = null;
		_reachWreckSubscription?.Dispose();
		_reachWreckSubscription = null;
		_fleeSubscription?.Dispose();
		_fleeSubscription = null;
		DiscardPreparedBattle();
		if (StarSystem is not null)
			StarSystem.ResourceTransactionCommitted -= OnResourceTransactionForStatistics;
	}

	private void OnResourceTransactionForStatistics(Transaction transaction) =>
		Statistics.RecordTransaction(transaction);

	internal void ReceiveEngagementFact(Record<EngagementCommitted> record) =>
		OnCommittedEngagement(record);

	private void EnsureRegistryForFleets(IEnumerable<Fleet> fleets)
	{
		foreach (var fleet in fleets)
		{
			foreach (var declaration in fleet.Registrations)
				ItemInventory.Register(declaration, fleet.State.Id);
		}

		foreach (var fleet in fleets)
		{
			foreach (var member in fleet.Members)
			{
				if (ItemInventory.TryGet(member.Id, out _))
					continue;

				throw new InvalidOperationException(
					$"Ship '{member.Id}' on fleet '{fleet.State.Id}' is missing from run item inventory.");
			}
		}
	}

	private void OnCommittedFleetSpawned(Record<FleetSpawned> record)
	{
		foreach (var declaration in record.Value.Members)
			ItemInventory.Register(declaration, record.Value.FleetId);
	}

	private void OnPlayerShipEnlisted(Record<PlayerShipEnlisted> record)
	{
		if (!TryAcquirePlayerShip(record.Value.Declaration))
		{
			GameLog.Log(
				$"Ignoring player ship enlist for '{record.Value.Declaration.ShipId}'; "
				+ "run acquire validation failed after strategic enlist committed.");
		}
	}

	private void OnOwnedShipPurchased(Record<OwnedShipPurchased> record)
	{
		if (!TryAcquireOwnedShip(record.Value.Declaration))
		{
			GameLog.Log(
				$"Ignoring owned ship purchase for '{record.Value.Declaration.ShipId}'; "
				+ "run acquire validation failed after merchant purchase committed.");
		}
	}

	private void OnPilotHireCommitted(Record<PilotHireCommitted> record)
	{
		if (!TryAcquirePilotHire(record.Value))
		{
			GameLog.Log(
				$"Ignoring pilot hire for '{record.Value.Profile.Name}'; "
				+ "run acquire validation failed after hire committed.");
		}
	}

	internal bool TryAcquireOwnedShip(ShipSpawnDeclaration declaration)
	{
		ArgumentNullException.ThrowIfNull(declaration);
		if (!CanCompletePlayerShipAcquire(declaration))
			return false;

		ItemInventory.Register(declaration, PlayerFleetUnitId);
		return true;
	}

	internal bool TryAcquirePilotHire(PilotHireCommitted hire)
	{
		ArgumentNullException.ThrowIfNull(hire);
		if (!PilotRegistry.CanRegister)
			return false;

		PilotRegistry.Register(Pilot.FromRecruitProfile(null, hire.Profile));
		return true;
	}

	private void ApplyPlayerShipAcquire(
		ShipSpawnDeclaration declaration,
		bool createStarterPilot = false,
		Random? rng = null)
	{
		ApplyPlayerShipAcquireWithPilot(declaration, createStarterPilot, rng);
	}

	private void ApplyPlayerShipAcquireWithPilot(
		ShipSpawnDeclaration declaration,
		bool createStarterPilot,
		Random? rng = null)
	{
		ItemInventory.Register(declaration, PlayerFleetUnitId);
		var shipClass = ShipCatalog.SpecFor(declaration.Chassis).ClassId;
		if (shipClass == EOperatorClassId.Unclassified)
			return;
		if (!PilotRegistry.CanRegister)
			return;

		var pilot = createStarterPilot
			? Pilot.CreateStarterForShip(declaration.ShipId, rng, shipClass)
			: Pilot.CreateForShip(declaration.ShipId, shipClass, rng);
		PilotRegistry.Register(pilot);
	}

	private void RefreshStrategicFleetProjection()
	{
		if (!StarSystem.Map.FleetRegistry.TryGet(PlayerFleetUnitId, out var fleet))
			return;

		var fieldedIds = GetFieldedShipIds();
		var registrationsByShipId = fleet.Registrations
			.ToDictionary(declaration => declaration.ShipId, StringComparer.Ordinal);
		var members = new FleetMember[fieldedIds.Count];
		var registrations = new ShipSpawnDeclaration[fieldedIds.Count];
		for (var index = 0; index < fieldedIds.Count; index++)
		{
			var shipId = fieldedIds[index];
			members[index] = new FleetMember(shipId);
			if (registrationsByShipId.TryGetValue(shipId, out var declaration))
			{
				registrations[index] = declaration;
				continue;
			}

			var ship = ItemInventory.Get(shipId);
			registrations[index] = new ShipSpawnDeclaration(
				shipId,
				ship.Spec.Chassis,
				ShipLoadoutGearTier.FromLoadout(ship.Loadout, ship.Spec.Chassis));
		}

		StarSystem.Map.FleetRegistry.Replace(new Fleet(fleet.State, members, registrations));
	}

	private void OnMerchantShipPurchase(Record<MerchantShipPurchase> record)
	{
		var purchase = record.Value;
		if (!ItemInventory.IsOwnedBy(purchase.ShipId, PlayerFleetUnitId))
		{
			GameLog.Log(
				$"Ignoring merchant ship purchase for '{purchase.ShipId}'; ship is not player-owned.");
			return;
		}

		if (!ItemInventory.TryGet(purchase.ShipId, out var current))
		{
			GameLog.Log(
				$"Ignoring merchant ship purchase for '{purchase.ShipId}'; ship is missing from the registry.");
			return;
		}

		if (!ItemInventory.Matches(purchase.ShipId, purchase.Before))
		{
			GameLog.Log(
				$"Ignoring merchant ship purchase for '{purchase.ShipId}'; registry no longer matches purchase snapshot.");
			return;
		}

		ItemInventory.Update(purchase.After.Clone());
	}

	private IReadOnlyList<SalaryPayrollCandidate> SnapshotSalaryPayrollRoster()
	{
		var roster = new List<SalaryPayrollCandidate>(PilotRegistry.All.Count);
		foreach (var pilot in PilotRegistry.All)
		{
			roster.Add(
				new SalaryPayrollCandidate(
					pilot.Id,
					pilot.BaseSalaryCredits,
					pilot.ClaimedLevel,
					pilot.MissedSalaryPeriods));
		}

		return roster;
	}

	private int SnapshotFleetSalaryDiscountStacks() =>
		PilotRegistry.QueryCapabilities(capabilityId =>
			capabilityId == EPilotCapabilityId.FleetSalaryDiscount).Count;

	private void OnPilotHazardPayroll(Record<HazardPayrollResult> record)
	{
		var payroll = record.Value;
		foreach (var pilotId in payroll.PaidPilotIds)
		{
			if (!PilotRegistry.TryGet(pilotId, out var pilot))
				continue;

			if (pilot.MissedHazardPeriods == 0)
				continue;

			PilotRegistry.Update(pilot.WithHazardMissedPeriods(0));
		}

		foreach (var miss in payroll.Misses)
		{
			if (!PilotRegistry.TryGet(miss.PilotId, out var pilot))
				continue;

			PilotRegistry.Update(pilot.WithHazardMissedPeriods(miss.MissedHazardPeriods));
		}
	}

	private IReadOnlySet<string>? ShipIdsExcludedByRefusedPilots(IReadOnlyList<string>? refusedPilotIds)
	{
		if (refusedPilotIds is null || refusedPilotIds.Count == 0)
			return null;

		var excluded = new HashSet<string>(StringComparer.Ordinal);
		foreach (var pilotId in refusedPilotIds)
		{
			if (!PilotRegistry.TryGet(pilotId, out var pilot)
				|| string.IsNullOrEmpty(pilot.AssignedShipId))
				continue;

			excluded.Add(pilot.AssignedShipId);
			GameLog.Log($"Pilot '{pilot.Name}' refused this engagement.");
		}

		return excluded.Count == 0 ? null : excluded;
	}

	private void OnPilotSalaryPayroll(Record<PilotSalaryPayroll> record)
	{
		var payroll = record.Value;
		var rosterChanged = false;

		foreach (var pilotId in payroll.PaidPilotIds)
		{
			if (!PilotRegistry.TryGet(pilotId, out var pilot))
				continue;

			if (pilot.MissedSalaryPeriods == 0)
				continue;

			PilotRegistry.Update(pilot.WithSalaryPayroll(0));
		}

		foreach (var miss in payroll.Misses)
		{
			if (!PilotRegistry.TryGet(miss.PilotId, out var pilot))
				continue;

			if (miss.Departed)
			{
				PilotRegistry.Remove(miss.PilotId);
				rosterChanged = true;
				GameLog.Log(
					$"Pilot '{pilot.Name}' departed after unpaid salary "
					+ $"(missed periods={miss.MissedSalaryPeriods}).");
				continue;
			}

			PilotRegistry.Update(pilot.WithSalaryPayroll(miss.MissedSalaryPeriods));
		}

		if (!rosterChanged)
			return;

		RefreshFieldedRoster();
	}

	private void OnPilotWalkedOut(Record<string> record)
	{
		if (!PilotRegistry.TryGet(record.Value, out var pilot))
			return;

		PilotRegistry.Remove(pilot.Id);
		GameLog.Log($"Pilot '{pilot.Name}' walked after the fleet refused their stake.");
		RefreshFieldedRoster();
	}

	private void RefreshFieldedRoster()
	{
		RefreshStrategicFleetProjection();
		RefreshStrategicPilotCapabilities();
	}

	private void OnFieldedShipDamage(Record<FieldedShipDamage> record)
	{
		var rosterChanged = false;
		foreach (var shipDamage in record.Value.Ships)
		{
			if (!CanApplyFieldedShipChange(shipDamage.ShipId, shipDamage.Before, record.Value.Cause))
				continue;

			if (shipDamage.After.CurrentHullPoints <= 0)
			{
				PilotRegistry.RemoveForShip(shipDamage.ShipId);
				ItemInventory.Remove(shipDamage.ShipId);
				rosterChanged = true;
				continue;
			}

			ItemInventory.Update(shipDamage.After.Clone());
		}

		if (!rosterChanged)
			return;

		RefreshStrategicFleetProjection();
		RefreshStrategicPilotCapabilities();
	}

	private bool CanApplyFieldedShipChange(string shipId, ShipInstance before, string source)
	{
		if (!GetFieldedShipIds().Contains(shipId, StringComparer.Ordinal))
		{
			GameLog.Log($"Ignoring {source} for '{shipId}'; ship is not fielded.");
			return false;
		}

		if (!ItemInventory.TryGet(shipId, out _))
		{
			GameLog.Log($"Ignoring {source} for '{shipId}'; ship is missing from the registry.");
			return false;
		}

		if (!ItemInventory.Matches(shipId, before))
		{
			GameLog.Log($"Ignoring {source} for '{shipId}'; registry no longer matches snapshot.");
			return false;
		}

		return true;
	}

	private void OnCommittedEngagement(Record<EngagementCommitted> record)
	{
		var fact = record.Value;
		if (!fact.ParticipantFleetIds.Contains(PlayerFleetUnitId, StringComparer.Ordinal))
			return;

		if (ActiveBattle is { Id: var activeBattleId }
			&& string.Equals(activeBattleId, fact.EngagementId, StringComparison.Ordinal))
			return;

		if (_battleLaunch is { } launching
			&& string.Equals(launching.Fact.EngagementId, fact.EngagementId, StringComparison.Ordinal))
			return;

		if (_resolvedBattleIds.Contains(fact.EngagementId))
		{
			ReleaseResolvedBattle(fact.EngagementId);
			GameLog.Log(
				$"Ignored relaunch of resolved engagement '{fact.EngagementId}'.");
			return;
		}

		if (ActiveBattle is not null)
		{
			GameLog.Log(
				$"Replacing stale active battle '{ActiveBattle.Id}' to launch engagement '{fact.EngagementId}'.");
			ActiveBattle = null;
			ReleaseBattleOutcomeSubscription();
		}

		if (_launchedEngagementIds.Contains(fact.EngagementId))
		{
			if (ActiveBattle is not null)
				return;

			_launchedEngagementIds.Remove(fact.EngagementId);
			GameLog.Log(
				$"Retrying battle launch for engagement '{fact.EngagementId}' (no active battle in run state).");
		}

		if (fact.DeployedPilotIds is { } deployed)
			PilotRegistry.RecordDeployment(deployed);

		var build = TakePreparedBattle(fact);
		if (build is { IsCompleted: false })
		{
			_battleLaunch = (fact, build);
			BattleReady?.Invoke();
			return;
		}

		if (LaunchBattle(fact, build))
			BattleReady?.Invoke();
	}

	public bool IsBattleLaunching => _battleLaunch is { Build.IsCompleted: false };

	public void CompleteBattleLaunch()
	{
		if (_battleLaunch is not { } launch || !launch.Build.IsCompleted)
			return;

		_battleLaunch = null;
		LaunchBattle(launch.Fact, launch.Build);
	}

	private bool LaunchBattle(EngagementCommitted fact, Task<PreparedBattleBuild>? build)
	{
		try
		{
			if (build is { IsCompletedSuccessfully: true })
			{
				_adoptedBattle = build.Result;
				ActiveBattle = build.Result.Encounter;
			}
			else
			{
				if (build?.Exception is { } buildError)
					GameLog.LogException(buildError, "Prepared battle failed; building synchronously.");
				ActiveBattle = CreateEncounter(fact);
			}

			_launchedEngagementIds.Add(fact.EngagementId);
			return true;
		}
		catch (Exception ex)
		{
			GameLog.LogException(ex, "Failed to construct battle from committed engagement.");
			StarSystem.RollbackFailedEngagementLaunch(fact, PlayerFleetUnitId);
			return false;
		}
	}

	private BattleEncounter CreateEncounter(EngagementCommitted fact)
	{
		var fleets = fact.ParticipantFleetIds
			.Select(id => StarSystem.Map.FleetRegistry.FleetOf(id))
			.ToArray();
		EnsureRegistryForFleets(fleets);
		var tutorialScriptedSpawn = TutorialBeatContracts.RequiresCanonicalFirstBattleLayout(
			_tutorialState,
			StarSystem.Map,
			fact.ParticipantFleetIds);
		var excludedShipIds = ShipIdsExcludedByRefusedPilots(fact.RefusedPilotIds);
		IReadOnlySet<string>? playerDeployShipIds = null;
		if (excludedShipIds is not null || fact.DeployedPilotIds is not null)
		{
			playerDeployShipIds = DeployedShipIds(fact)
				.Where(shipId => excludedShipIds?.Contains(shipId) != true)
				.ToHashSet(StringComparer.Ordinal);
		}

		return EngagementBattleFactory.Create(
			fleets,
			ItemInventory,
			Random.Shared.Next(),
			fact.EngagementId,
			fact.InitiatorFleetId,
			BuildPilotBattleSnapshots(playerDeployShipIds),
			tutorialScriptedSpawn: tutorialScriptedSpawn,
			playerDeployShipIds: playerDeployShipIds);
	}

	private void PrepareBattle()
	{
		DiscardPreparedBattle();
		var map = StarSystem.Map;
		if (!map.FleetRegistry.TryGet(PlayerFleetUnitId, out var player)
			|| player.State.CurrentEngagement is not { Phase: EEngagementPhase.AwaitingDecision } engagement
			|| EngagementQueries.ResolveCounterpartyId(player.State) is not { } counterpartyId
			|| !map.FleetRegistry.TryGet(counterpartyId, out var counterparty))
			return;

		try
		{
			var fleets = new[] { player, counterparty }
				.OrderBy(fleet => fleet.State.Id, StringComparer.Ordinal)
				.ToArray();
			var participantIds = fleets.Select(fleet => fleet.State.Id).ToArray();
			EnsureRegistryForFleets(fleets);
			var inventory = new Inventory();
			foreach (var fleet in fleets)
			{
				foreach (var member in fleet.Members)
					inventory.Register(ItemInventory.Get(member.Id).Clone(), fleet.State.Id);
			}

			var initiatorId = EngagementQueries.ResolveInitiatorId(
				PlayerFleetUnitId, player.State, counterpartyId, counterparty.State);
			var pilots = BuildPilotBattleSnapshots(null);
			var tutorialScriptedSpawn = TutorialBeatContracts.RequiresCanonicalFirstBattleLayout(
				_tutorialState, map, participantIds);
			var seed = Random.Shared.Next();
			var engagementId = engagement.Id;

			_preparedBattle = new PreparedBattle(
				engagementId,
				initiatorId,
				participantIds,
				Crews(GetFieldedShipIds()),
				Task.Run(() =>
				{
					var encounter = EngagementBattleFactory.Create(
						fleets,
						inventory,
						seed,
						engagementId,
						initiatorId,
						pilots,
						tutorialScriptedSpawn: tutorialScriptedSpawn);
					var battle = BattleOrchestrator.PrepareFromEncounter(encounter);
					battle.Preload("encounter ready");
					if (battle.Phase == EBattlePhase.PlayerTurn)
						battle.MovePreview.GetPaths(battle.PlayerAgent.Sim, battle.PlayerId);
					return new PreparedBattleBuild(encounter, battle);
				}));
		}
		catch (Exception ex)
		{
			GameLog.LogException(ex, "Failed to prepare battle for pending engagement.");
		}
	}

	private Task<PreparedBattleBuild>? TakePreparedBattle(EngagementCommitted fact)
	{
		var prepared = _preparedBattle;
		_preparedBattle = null;
		if (prepared is null)
			return null;

		if (string.Equals(prepared.EngagementId, fact.EngagementId, StringComparison.Ordinal)
			&& string.Equals(prepared.InitiatorFleetId, fact.InitiatorFleetId, StringComparison.Ordinal)
			&& prepared.ParticipantFleetIds.SequenceEqual(fact.ParticipantFleetIds, StringComparer.Ordinal)
			&& fact.RefusedPilotIds is null
			&& prepared.Crews.SetEquals(Crews(DeployedShipIds(fact))))
			return prepared.Build;

		DisposeWhenBuilt(prepared.Build);
		return null;
	}

	private IEnumerable<string> DeployedShipIds(EngagementCommitted fact) =>
		fact.DeployedPilotIds is null
			? GetFieldedShipIds()
			: fact.DeployedPilotIds
				.Select(id => PilotRegistry.TryGet(id, out var pilot) ? pilot.AssignedShipId : null)
				.OfType<string>();

	/// <summary>Pilot-to-ship pairs; a prepared battle is stale once any pair changes.</summary>
	private HashSet<string> Crews(IEnumerable<string> shipIds) =>
		shipIds
			.Select(shipId => PilotRegistry.TryGetForShip(shipId, out var pilot)
				? $"{pilot.Id}:{shipId}"
				: shipId)
			.ToHashSet(StringComparer.Ordinal);

	private void DiscardPreparedBattle()
	{
		if (_preparedBattle is { } prepared)
			DisposeWhenBuilt(prepared.Build);
		_preparedBattle = null;
	}

	private static void DisposeWhenBuilt(Task<PreparedBattleBuild> build) =>
		build.ContinueWith(
			task => task.Result.Battle.Dispose(),
			TaskContinuationOptions.OnlyOnRanToCompletion);

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

			if (!ItemInventory.TryGet(handoff.Id, out var ship))
				throw new InvalidOperationException(
					$"Battle outcome handoff '{handoff.Id}' has no registry row.");

			if (ship.Spec.Chassis != handoff.Chassis)
				throw new InvalidOperationException(
					$"Battle outcome chassis mismatch for ship '{handoff.Id}'.");
		}
	}

	private void GrantPilotBattleXp(
		IReadOnlyList<BattleSpawn> spawns,
		int xpDifficulty,
		BattleOutcome outcome)
	{
		foreach (var (pilotId, xp) in PilotProgression.RewardsFor(
			         spawns,
			         outcome,
			         xpDifficulty,
			         id => PilotRegistry.TryGet(id, out var pilot)
				         ? pilot.LearnedSkills
				         : Array.Empty<EPilotSkillId>(),
			         id => PilotRegistry.TryGet(id, out var pilot)
				         ? pilot.BackgroundId
				         : null))
		{
			if (!PilotRegistry.TryGet(pilotId, out var pilot))
				continue;

			var updated = PilotProgression.AddExperience(pilot, xp);
			PilotRegistry.Update(updated);
			GameLog.Log(
				$"Pilot XP: '{updated.Name}' +{xp} → Lv {PilotProgression.LevelFor(updated.ExperiencePoints)} "
				+ $"({updated.ExperiencePoints} total).");
		}
	}

	private void ApplyOutcomeToRegistry(BattleOutcome outcome)
	{
		var playerRosterChanged = false;
		foreach (var handoff in outcome.StateHandoffs)
		{
			if (handoff.CurrentHullPoints <= 0 && ItemInventory.IsPlayerOwned(handoff.Id))
			{
				PilotRegistry.RemoveForShip(handoff.Id);
				ItemInventory.Remove(handoff.Id);
				playerRosterChanged = true;
				continue;
			}

			if (!ItemInventory.TryGet(handoff.Id, out _))
				continue;

			ItemInventory.ApplyHandoff(handoff);
		}

		if (!playerRosterChanged)
			return;

		RefreshStrategicFleetProjection();
		RefreshStrategicPilotCapabilities();
	}

	private void RefreshStrategicPilotCapabilities() =>
		StarSystem.CommitSetup(new RefreshStrategicPilotCapabilitiesAction(
			PlayerFleetUnitId,
			PilotRegistry.QueryCapabilitiesForAssigned(
				StarMapPilotCapabilityPipelineFactory.Supports)));

	private void SyncStarPlayerPipeline() =>
		StarMapPilotCapabilityPipelineFactory.InstallPlayerPipeline(StarSystem, PilotRegistry);

	private IReadOnlyDictionary<string, PilotBattleSnapshot> BuildPilotBattleSnapshots(
		IReadOnlySet<string>? playerDeployShipIds = null)
	{
		var map = new Dictionary<string, PilotBattleSnapshot>(StringComparer.Ordinal);
		foreach (var pilot in PilotRegistry.Assigned)
		{
			var shipId = pilot.AssignedShipId!;
			if (playerDeployShipIds is not null && !playerDeployShipIds.Contains(shipId))
				continue;

			var battleGrants = PilotRegistry.QueryCapabilitiesForShip(
				shipId,
				BattlePilotCapabilityPipelineFactory.Supports);
			map[shipId] = new PilotBattleSnapshot(
				pilot.Id,
				battleGrants,
				pilot.PortraitId);
		}

		return map;
	}

	private void ValidatePilotAssignments()
	{
		var assignedShips = new HashSet<string>(StringComparer.Ordinal);
		foreach (var pilot in PilotRegistry.All)
		{
			if (string.IsNullOrEmpty(pilot.AssignedShipId))
				continue;

			if (!assignedShips.Add(pilot.AssignedShipId))
				throw new InvalidDataException(
					$"Ship '{pilot.AssignedShipId}' is assigned to multiple pilots.");

			if (!ItemInventory.TryGet(pilot.AssignedShipId, out var ship))
				throw new InvalidDataException(
					$"Pilot '{pilot.Id}' is assigned to missing ship '{pilot.AssignedShipId}'.");

			if (!ItemInventory.IsPlayerOwned(pilot.AssignedShipId))
				throw new InvalidDataException(
					$"Pilot '{pilot.Id}' is assigned to non-player ship '{pilot.AssignedShipId}'.");

			if (!OperatorClassCompatibility.CanOperate(pilot.ClassId, ship.Spec.ClassId))
				throw new InvalidDataException(
					$"Pilot '{pilot.Id}' with class '{pilot.ClassId}' cannot operate ship "
					+ $"'{pilot.AssignedShipId}' with class '{ship.Spec.ClassId}'.");
		}
	}

	private void ReleaseBattleOutcomeSubscription()
	{
		_battleOutcomeSubscription?.Dispose();
		_battleOutcomeSubscription = null;
	}
}
