using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.Math;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class ContractBoardExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
{
	private const int WreckageMinimumPoiClearance = 16;
	private const float WreckageBorderReferenceWeight = 0.75f;

	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly Func<bool> _generationEnabled;
	private readonly ContractBoardConfig _config;
	private readonly ContractPlacement _placement;
	private readonly ContractNarrativePicker _narrativePicker;

	public ContractBoardExecutionAgent(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		Func<bool> generationEnabled,
		ContractBoardConfig? config = null)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_generationEnabled = generationEnabled;
		_config = config ?? new ContractBoardConfig();
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_config.ContractGiverLeaseTicks);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_config.ContractOperatorVisitPauseTicks);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_config.PreferredContractsPerGiver);
		ValidateLeadGenerationConfig(_config.Leads);
		_placement = new ContractPlacement(_config.Placement);
		_narrativePicker = new ContractNarrativePicker(_config.Narrative);
	}

	public void PlanAndPublish()
	{
		if (!_canWork || _actorId is null)
			return;

		ClearBatchInFlight();
		var world = _world();
		var tick = world.Timeline.Clock.Current;
		var action = BuildMaintainAction(world, tick);
		var runtime = _runtimeFor(_actorId);
		if (!MaintainContractBoardDef.Instance.IsLegal(action, world, runtime))
			return;

		Publish([action]);
	}

	public void ReconcilePresentations()
	{
		var map = _world();
		var tick = map.Timeline.Clock.Current;
		var pendingIds = map.ContractRegistry.Pending
			.Where(contract =>
				contract.Category == EContractCategory.Standard
				&& !contract.IsStoryObjective)
			.Select(contract => contract.Id)
			.ToHashSet(StringComparer.Ordinal);
		var pendingStoryIds = map.ContractRegistry.Pending
			.Where(contract =>
				contract.Category == EContractCategory.Standard
				&& contract.IsStoryObjective)
			.Select(contract => contract.Id)
			.ToHashSet(StringComparer.Ordinal);
		var pendingLeadIds = map.ContractRegistry.Pending
			.Where(contract => contract.Category == EContractCategory.Lead)
			.Select(contract => contract.Id)
			.ToHashSet(StringComparer.Ordinal);

		foreach (var poi in map.PointsOfInterest)
		{
			poi.OperatorTemporaryRoles.PruneExpiredContractPlacementPauses(tick);
			poi.OperatorTemporaryRoles.PruneSources(EFacilityOperatorRole.Contracts, pendingIds);
			poi.OperatorTemporaryRoles.PruneSources(
				EFacilityOperatorRole.StoryContact,
				pendingStoryIds);
			poi.OperatorTemporaryRoles.PruneSources(
				EFacilityOperatorRole.LeadContact,
				pendingLeadIds);
		}

		var assignedIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var (location, assignment) in ListRoleOperators(
			map,
			EFacilityOperatorRole.Contracts))
		{
			foreach (var contractId in assignment.SourceIds)
			{
				if (assignedIds.Add(contractId))
					continue;

				map.GetPointOfInterest(location.PoiId).OperatorTemporaryRoles.RevokeSource(
					location.FacilityId,
					location.OperatorName,
					contractId);
			}
		}

		foreach (var contractId in pendingIds
			.Where(contractId => !assignedIds.Contains(contractId))
			.Order(StringComparer.Ordinal))
		{
			var target = FindAvailableContractOperator(map, tick, contractId);
			if (target is null)
			{
				GameLog.Log(
					$"[contracts] unable to present pending contract '{contractId}': no available operator.");
				continue;
			}

			var roles = map.GetPointOfInterest(target.Location.PoiId).OperatorTemporaryRoles;
			if (target.StartsRole)
			{
				roles.Grant(
					target.Location.FacilityId,
					target.Location.OperatorName,
					EFacilityOperatorRole.Contracts,
					contractId,
					tick + _config.ContractGiverLeaseTicks);
			}
			else
			{
				if (target.RenewsLease)
				{
					roles.RenewSourceAcceptance(
						target.Location.FacilityId,
						target.Location.OperatorName,
						tick + _config.ContractGiverLeaseTicks);
				}

				roles.Grant(
					target.Location.FacilityId,
					target.Location.OperatorName,
					EFacilityOperatorRole.Contracts,
					contractId);
			}

			assignedIds.Add(contractId);
		}

		ReconcileLeadContacts(map, tick, pendingLeadIds);
	}

	public bool TryPauseContractPresentation(
		string poiId,
		string facilityId,
		string operatorName)
	{
		var map = _world();
		if (!map.TryGetPointOfInterest(poiId, out var poi))
			return false;

		Facility facility;
		try
		{
			facility = poi.GetFacility(facilityId);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		var facilityOperator = facility.Operators.FirstOrDefault(candidate =>
			string.Equals(candidate.Name, operatorName, StringComparison.OrdinalIgnoreCase));
		if (facilityOperator is null)
			return false;

		var hasTemporaryRole = poi.OperatorTemporaryRoles.TryGetRole(
			facilityId,
			facilityOperator.Name,
			out var role);
		if (hasTemporaryRole
			&& role is EFacilityOperatorRole.StoryContact
				or EFacilityOperatorRole.LeadContact)
			return true;

		if ((!hasTemporaryRole || role != EFacilityOperatorRole.Contracts)
			&& !map.ContractRegistry.Pending.Any(contract =>
				contract.Category == EContractCategory.Standard))
			return false;

		poi.OperatorTemporaryRoles.PauseContractPlacement(
			facilityId,
			facilityOperator.Name,
			map.Timeline.Clock.Current + _config.ContractOperatorVisitPauseTicks);
		return true;
	}

	private ContractOperatorTarget? FindAvailableContractOperator(
		StarMap map,
		int tick,
		string contractId)
	{
		if (!map.ContractRegistry.TryGet(contractId, out var contract))
			return null;

		var deliveryTurnInPoiId = contract.Objective is DeliveryObjective delivery
			? delivery.TurnInPoiId
			: null;

		var current = ListRoleOperators(map, EFacilityOperatorRole.Contracts);
		var viable = current
			.Where(item => IsActiveContractOperator(map, item.Location))
			.Where(item =>
				item.Assignment.AcceptsSourcesUntilTick is null
				|| tick < item.Assignment.AcceptsSourcesUntilTick)
			.Where(item => !IsPlacementPaused(map, item.Location, tick))
			.Where(item => item.Assignment.SourceIds.Count < _config.PreferredContractsPerGiver)
			.Where(item => !IsDeliveryTurnInPoi(deliveryTurnInPoiId, item.Location))
			.ToArray();
		if (viable.Length > 0)
		{
			var selected = Pick(viable, map.Seed, tick, contractId, "existing-contract-operator");
			return new ContractOperatorTarget(selected.Location, false, false);
		}

		var eligible = ListOperators(map)
			.Where(candidate => candidate.Operator.Role is not (
				EFacilityOperatorRole.Merchant
				or EFacilityOperatorRole.HealingStation))
			.Where(candidate => !HasTemporaryRole(map, candidate.Location))
			.Where(candidate => !IsPlacementPaused(map, candidate.Location, tick))
			.Where(candidate => !IsDeliveryTurnInPoi(deliveryTurnInPoiId, candidate.Location))
			.ToArray();
		if (eligible.Length > 0)
		{
			var selected = Pick(eligible, map.Seed, tick, contractId, "new-contract-operator");
			return new ContractOperatorTarget(selected.Location, true, false);
		}

		var fallback = current
			.Where(item => IsActiveContractOperator(map, item.Location))
			.Where(item => !IsPlacementPaused(map, item.Location, tick))
			.Where(item => !IsDeliveryTurnInPoi(deliveryTurnInPoiId, item.Location))
			.ToArray();
		if (fallback.Length == 0)
			return null;

		var minimumLoad = fallback.Min(item => item.Assignment.SourceIds.Count);
		var leastLoaded = fallback
			.Where(item => item.Assignment.SourceIds.Count == minimumLoad)
			.ToArray();
		var reused = Pick(leastLoaded, map.Seed, tick, contractId, "fallback-contract-operator");
		GameLog.Log(
			$"[contracts] warning: no idle NPC available; renewing contract operator " +
			$"'{reused.Location.OperatorName}' at '{reused.Location.PoiId}'.");
		return new ContractOperatorTarget(reused.Location, false, true);
	}

	private void ReconcileLeadContacts(
		StarMap map,
		int tick,
		IReadOnlySet<string> pendingLeadIds)
	{
		var assignedIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var (location, assignment) in ListRoleOperators(
			map,
			EFacilityOperatorRole.LeadContact))
		{
			var locationEligible = IsEligibleLeadContactOperator(map, location);
			foreach (var contractId in assignment.SourceIds)
			{
				if (locationEligible && assignedIds.Add(contractId))
				{
					locationEligible = false;
					continue;
				}

				map.GetPointOfInterest(location.PoiId).OperatorTemporaryRoles.RevokeSource(
					location.FacilityId,
					location.OperatorName,
					contractId);
			}
		}

		foreach (var contractId in pendingLeadIds
			.Where(contractId => !assignedIds.Contains(contractId))
			.Order(StringComparer.Ordinal))
		{
			if (!map.ContractRegistry.TryGet(contractId, out var contract))
				continue;

			var target = FindAvailableLeadContact(map, tick, contract);
			if (target is null)
			{
				GameLog.Log(
					$"[contracts] unable to present pending lead '{contractId}': no available dialog operator.");
				continue;
			}

			map.GetPointOfInterest(target.Location.PoiId).OperatorTemporaryRoles.Grant(
				target.Location.FacilityId,
				target.Location.OperatorName,
				EFacilityOperatorRole.LeadContact,
				contractId);
			assignedIds.Add(contractId);
		}
	}

	private static OperatorCandidate? FindAvailableLeadContact(
		StarMap map,
		int tick,
		Contract contract)
	{
		var eligible = ListOperators(map)
			.Where(candidate => candidate.Operator.Role == EFacilityOperatorRole.Dialog)
			.Where(candidate => !HasAnyTemporaryAssignment(map, candidate.Location))
			.Where(candidate => !IsPlacementPaused(map, candidate.Location, tick))
			.ToArray();
		if (eligible.Length == 0)
			return null;

		var destinationPoiId = (contract.Objective as BlackMarketSaleObjective)?.Destination
			is BlackMarketFacilityDestination facilityDestination
				? facilityDestination.PoiId
				: null;
		var preferred = destinationPoiId is null
			? eligible
			: eligible
				.Where(candidate => !string.Equals(
					candidate.Location.PoiId,
					destinationPoiId,
					StringComparison.Ordinal))
				.ToArray();
		var candidates = preferred.Length > 0 ? preferred : eligible;
		return Pick(candidates, map.Seed, tick, contract.Id, "lead-contact-operator");
	}

	private static IReadOnlyList<(
		OperatorLocation Location,
		FacilityOperatorTemporaryRoles.Assignment Assignment)> ListRoleOperators(
			StarMap map,
			EFacilityOperatorRole role) =>
		map.PointsOfInterest
			.OrderBy(poi => poi.Id, StringComparer.Ordinal)
			.SelectMany(poi => poi.OperatorTemporaryRoles
				.AssignmentsIncludingSuspended(role)
				.Select(assignment => (
					new OperatorLocation(poi.Id, assignment.FacilityId, assignment.OperatorName),
					assignment)))
			.ToArray();

	private static IReadOnlyList<OperatorCandidate> ListOperators(StarMap map) =>
		map.PointsOfInterest
			.OrderBy(poi => poi.Id, StringComparer.Ordinal)
			.SelectMany(poi => poi.Facilities
				.OrderBy(facility => facility.Id, StringComparer.Ordinal)
				.SelectMany(facility => facility.Operators
					.OrderBy(facilityOperator => facilityOperator.Name, StringComparer.Ordinal)
					.Select(facilityOperator => new OperatorCandidate(
						new OperatorLocation(poi.Id, facility.Id, facilityOperator.Name),
						facilityOperator))))
			.ToArray();

	private static bool HasTemporaryRole(StarMap map, OperatorLocation location) =>
		map.GetPointOfInterest(location.PoiId).OperatorTemporaryRoles.TryGetRole(
			location.FacilityId,
			location.OperatorName,
			out _);

	private static bool HasAnyTemporaryAssignment(StarMap map, OperatorLocation location) =>
		map.GetPointOfInterest(location.PoiId).OperatorTemporaryRoles.HasAssignment(
			location.FacilityId,
			location.OperatorName);

	private static bool IsEligibleLeadContactOperator(StarMap map, OperatorLocation location)
	{
		var poi = map.GetPointOfInterest(location.PoiId);
		var facility = poi.Facilities.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, location.FacilityId, StringComparison.Ordinal));
		return facility?.Operators.Any(candidate =>
			string.Equals(candidate.Name, location.OperatorName, StringComparison.Ordinal)
			&& candidate.Role == EFacilityOperatorRole.Dialog) == true;
	}

	private static bool IsActiveContractOperator(StarMap map, OperatorLocation location) =>
		map.GetPointOfInterest(location.PoiId).OperatorTemporaryRoles.TryGetRole(
			location.FacilityId,
			location.OperatorName,
			out var role)
		&& role == EFacilityOperatorRole.Contracts;

	private static bool IsPlacementPaused(
		StarMap map,
		OperatorLocation location,
		int tick) =>
		map.GetPointOfInterest(location.PoiId).OperatorTemporaryRoles.IsContractPlacementPaused(
			location.FacilityId,
			location.OperatorName,
			tick);

	private static bool IsDeliveryTurnInPoi(string? turnInPoiId, OperatorLocation location) =>
		turnInPoiId is not null
		&& string.Equals(turnInPoiId, location.PoiId, StringComparison.Ordinal);

	private static T Pick<T>(
		IReadOnlyList<T> candidates,
		int mapSeed,
		int tick,
		string contractId,
		string stream)
	{
		var random = new StableRandom(
			StableSeedMixer.From(mapSeed)
				.Add(tick)
				.Add(contractId)
				.Add(stream)
				.Value);
		return candidates[(int)(random.NextDouble() * candidates.Count)];
	}

	private MaintainContractBoardAction BuildMaintainAction(StarMap map, int tick)
	{
		var additions = new List<ContractAddition>();
		if (_generationEnabled() && IsCadenceTick(tick))
		{
			var slots = SlotsToFill(map);
			var queuedContracts = new List<Contract>();
			for (var slot = 0; slot < slots; slot++)
			{
				if (!TryBuildAddition(map, tick, slot, queuedContracts, out var addition))
					continue;

				if (!CanRegisterAfterPriorAdditions(map, additions, addition))
					continue;

				additions.Add(addition);
				queuedContracts.Add(addition.Contract);
			}

			if (TryBuildLeadAddition(map, tick, additions, out var leadAddition))
				additions.Add(leadAddition);
		}

		return new MaintainContractBoardAction(StarSystemActorIds.Contracts, tick, additions);
	}

	private bool IsCadenceTick(int tick) =>
		_config.CadenceTicks > 0 && tick % _config.CadenceTicks == 0;

	private int SlotsToFill(StarMap map)
	{
		var target = _config.Placement.TargetGeneratedCount;
		var occupied = map.ContractRegistry.CountStandardBoardOccupancy();
		return System.Math.Max(0, target - occupied);
	}

	private bool TryBuildAddition(
		StarMap map,
		int tick,
		int slot,
		IReadOnlyList<Contract> queuedBoardContracts,
		out ContractAddition addition)
	{
		addition = default!;
		var decision = _placement.Pick(map, tick, slot, queuedBoardContracts);
		if (decision is null)
			return false;

		var contractId = ContractIdFor(map, tick, slot);
		var danger = StarSystemDangerProgression.RollDanger(map, tick, slot, "contract-danger");
		if (!TryBuildContract(map, contractId, decision, danger, tick, slot, out var contract))
			return false;

		addition = new ContractAddition(contract, tick + _config.TtlTicks);
		return true;
	}

	private bool TryBuildLeadAddition(
		StarMap map,
		int tick,
		IReadOnlyList<ContractAddition> queued,
		out ContractAddition addition)
	{
		addition = default!;
		var queuedLeadCount = queued.Count(candidate =>
			candidate.Contract.Category == EContractCategory.Lead);
		if (map.ContractRegistry.CountLeadOccupancy(tick) + queuedLeadCount
			>= _config.Leads.MaxOccupancy)
			return false;

		var chanceRandom = new StableRandom(
			StableSeedMixer.From(map.Seed)
				.Add(tick)
				.Add("lead-generation-chance")
				.Value);
		if (chanceRandom.NextDouble() >= _config.Leads.GenerationChance)
			return false;

		var kind = PickLeadKind(map.Seed, tick, _config.Leads.KindWeights);
		var contractId = LeadContractIdFor(map, tick);
		if (!ContractFactory.TryBuildLead(
			map,
			contractId,
			new LeadCreateArgs(
				kind,
				_narrativePicker.Pick(map, EContractKind.Lead, tick, slotIndex: 0),
				_config.Leads.SaleDurationTicks,
				_config.Leads.SpaceMeetingChance),
			out var contract))
			return false;

		addition = new ContractAddition(contract, tick + _config.TtlTicks);
		return true;
	}

	private static bool CanRegisterAfterPriorAdditions(
		StarMap map,
		IReadOnlyList<ContractAddition> queued,
		ContractAddition next)
	{
		if (map.ContractRegistry.Contains(next.Contract.Id))
			return false;

		foreach (var addition in queued)
		{
			if (string.Equals(addition.Contract.Id, next.Contract.Id, StringComparison.Ordinal))
				return false;
		}

		var queuedStandardCount = queued.Count(addition =>
			addition.Contract.Category == EContractCategory.Standard);
		return map.ContractRegistry.CountPendingStandard() + queuedStandardCount
			< map.ContractRegistry.MaxPending;
	}

	private static string ContractIdFor(StarMap map, int tick, int slot) =>
		$"contract-gen-{StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("contract-id").Value:x}";

	private static string LeadContractIdFor(StarMap map, int tick) =>
		$"lead-gen-{StableSeedMixer.From(map.Seed).Add(tick).Add("lead-contract-id").Value:x}";

	private static ELeadKind PickLeadKind(
		int mapSeed,
		int tick,
		IReadOnlyDictionary<ELeadKind, float> weights)
	{
		var candidates = Enum.GetValues<ELeadKind>()
			.Select(kind => (Kind: kind, Weight: weights.GetValueOrDefault(kind)))
			.Where(candidate => candidate.Weight > 0)
			.ToArray();
		var totalWeight = candidates.Sum(candidate => candidate.Weight);
		var random = new StableRandom(
			StableSeedMixer.From(mapSeed)
				.Add(tick)
				.Add("lead-kind")
				.Value);
		var roll = random.NextDouble() * totalWeight;
		var cumulative = 0.0;
		foreach (var candidate in candidates)
		{
			cumulative += candidate.Weight;
			if (roll < cumulative)
				return candidate.Kind;
		}

		return candidates[^1].Kind;
	}

	private static void ValidateLeadGenerationConfig(LeadGenerationConfig config)
	{
		ArgumentNullException.ThrowIfNull(config);
		if (!double.IsFinite(config.GenerationChance)
			|| config.GenerationChance is < 0 or > 1)
			throw new ArgumentOutOfRangeException(
				nameof(config),
				config.GenerationChance,
				"Lead generation chance must be between zero and one.");
		if (!double.IsFinite(config.SpaceMeetingChance)
			|| config.SpaceMeetingChance is < 0 or > 1)
			throw new ArgumentOutOfRangeException(
				nameof(config),
				config.SpaceMeetingChance,
				"Lead space meeting chance must be between zero and one.");
		ArgumentOutOfRangeException.ThrowIfNegative(config.MaxOccupancy);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(config.SaleDurationTicks);
		ArgumentNullException.ThrowIfNull(config.KindWeights);

		foreach (var (kind, weight) in config.KindWeights)
		{
			if (!Enum.IsDefined(kind))
				throw new ArgumentOutOfRangeException(nameof(config), kind, null);
			if (!float.IsFinite(weight) || weight < 0)
				throw new ArgumentOutOfRangeException(
					nameof(config),
					weight,
					"Lead kind weights must be finite and non-negative.");
		}

		if (config.GenerationChance > 0
			&& config.MaxOccupancy > 0
			&& !config.KindWeights.Any(entry => entry.Value > 0))
			throw new ArgumentException(
				"At least one lead kind must have a positive generation weight.",
				nameof(config));
	}

	private bool TryBuildContract(
		StarMap map,
		string contractId,
		ContractPlacement.Decision decision,
		EDangerLevel danger,
		int tick,
		int slot,
		out Contract contract)
	{
		contract = null!;
		switch (decision.Kind)
		{
			case EContractKind.Hunt:
				return ContractFactory.TryBuildHunt(
					map,
					contractId,
					BuildHuntArgs(map, danger, tick, slot),
					out contract);
			case EContractKind.Delivery:
				contract = ContractFactory.Build(
					map,
					contractId,
					EContractKind.Delivery,
					BuildDeliveryArgs(map, contractId, danger, tick, slot));
				return true;
			case EContractKind.Wreckage:
				return ContractFactory.TryBuildWreckage(
					map,
					contractId,
					BuildWreckageArgs(map, danger, tick, slot),
					out contract);
			default:
				throw new ArgumentOutOfRangeException(nameof(decision.Kind), decision.Kind, null);
		}
	}

	private HuntCreateArgs BuildHuntArgs(
		StarMap map,
		EDangerLevel danger,
		int tick,
		int slot)
	{
		var landmarkIds = MapLandmarkQueries.AllIds(map);
		var areaPickMix = (long)StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("hunt-area").Value;
		var narrative = _narrativePicker.Pick(map, EContractKind.Hunt, tick, slot);

		return new HuntCreateArgs(
			new AreaPickerArgs(landmarkIds, DeterministicPickMix: areaPickMix),
			danger,
			narrative);
	}

	private DeliveryCreateArgs BuildDeliveryArgs(
		StarMap map,
		string contractId,
		EDangerLevel danger,
		int tick,
		int slot)
	{
		var narrative = _narrativePicker.Pick(map, EContractKind.Delivery, tick, slot);
		return new DeliveryCreateArgs(
			danger,
			narrative,
			Generation: new DeliveryGenerationConfig(
				StarSystemDangerProgression.RollDeliveryLegCount(map.Seed, contractId, danger)));
	}

	private WreckageCreateArgs BuildWreckageArgs(
		StarMap map,
		EDangerLevel danger,
		int tick,
		int slot)
	{
		var navLandmarkIds = map.NavigationLandmarks
			.Select(landmark => landmark.Id)
			.OrderBy(id => id, StringComparer.Ordinal)
			.ToArray();
		var areaPickMix = (long)StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("wreckage-area").Value;
		var modeRandom = new StableRandom(
			StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("wreckage-area-mode").Value);
		var referenceMode = modeRandom.NextDouble() < WreckageBorderReferenceWeight
			? EAreaPickerReferenceMode.LandmarkWithBorderTriangle
			: EAreaPickerReferenceMode.TriangulateLandmarks;
		var narrative = _narrativePicker.Pick(map, EContractKind.Wreckage, tick, slot);

		return new WreckageCreateArgs(
			new AreaPickerArgs(
				navLandmarkIds,
				WreckageMinimumPoiClearance,
				DeterministicPickMix: areaPickMix,
				ReferenceMode: referenceMode,
				BorderReferenceConfig: new AreaBorderReferenceConfig()),
			danger,
			narrative);
	}

	private sealed record OperatorLocation(
		string PoiId,
		string FacilityId,
		string OperatorName);

	private sealed record OperatorCandidate(
		OperatorLocation Location,
		FacilityOperator Operator);

	private sealed record ContractOperatorTarget(
		OperatorLocation Location,
		bool StartsRole,
		bool RenewsLease);

}
