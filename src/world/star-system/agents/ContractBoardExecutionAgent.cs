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
			.Where(contract => !contract.IsStoryObjective)
			.Select(contract => contract.Id)
			.ToHashSet(StringComparer.Ordinal);
		var pendingStoryIds = map.ContractRegistry.Pending
			.Where(contract => contract.IsStoryObjective)
			.Select(contract => contract.Id)
			.ToHashSet(StringComparer.Ordinal);

		foreach (var poi in map.PointsOfInterest)
		{
			poi.OperatorTemporaryRoles.PruneExpiredContractPlacementPauses(tick);
			poi.OperatorTemporaryRoles.PruneSources(EFacilityOperatorRole.Contracts, pendingIds);
			poi.OperatorTemporaryRoles.PruneSources(
				EFacilityOperatorRole.StoryContact,
				pendingStoryIds);
		}

		var assignedIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var (location, assignment) in ListContractOperators(map))
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
		if (hasTemporaryRole && role == EFacilityOperatorRole.StoryContact)
			return true;

		if ((!hasTemporaryRole || role != EFacilityOperatorRole.Contracts)
			&& !map.ContractRegistry.Pending.Any())
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
		var current = ListContractOperators(map);
		var viable = current
			.Where(item =>
				item.Assignment.AcceptsSourcesUntilTick is null
				|| tick < item.Assignment.AcceptsSourcesUntilTick)
			.Where(item => !IsPlacementPaused(map, item.Location, tick))
			.Where(item => item.Assignment.SourceIds.Count < _config.PreferredContractsPerGiver)
			.ToArray();
		if (viable.Length > 0)
		{
			var selected = Pick(viable, map.Seed, tick, contractId, "existing-contract-operator");
			return new ContractOperatorTarget(selected.Location, false, false);
		}

		var eligible = ListOperators(map)
			.Where(candidate => candidate.Operator.Role != EFacilityOperatorRole.Merchant)
			.Where(candidate => !HasTemporaryRole(map, candidate.Location))
			.Where(candidate => !IsPlacementPaused(map, candidate.Location, tick))
			.ToArray();
		if (eligible.Length > 0)
		{
			var selected = Pick(eligible, map.Seed, tick, contractId, "new-contract-operator");
			return new ContractOperatorTarget(selected.Location, true, false);
		}

		var fallback = current
			.Where(item => !IsPlacementPaused(map, item.Location, tick))
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

	private static IReadOnlyList<(
		OperatorLocation Location,
		FacilityOperatorTemporaryRoles.Assignment Assignment)> ListContractOperators(StarMap map) =>
		map.PointsOfInterest
			.OrderBy(poi => poi.Id, StringComparer.Ordinal)
			.SelectMany(poi => poi.OperatorTemporaryRoles
				.Assignments(EFacilityOperatorRole.Contracts)
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

	private static bool IsPlacementPaused(
		StarMap map,
		OperatorLocation location,
		int tick) =>
		map.GetPointOfInterest(location.PoiId).OperatorTemporaryRoles.IsContractPlacementPaused(
			location.FacilityId,
			location.OperatorName,
			tick);

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
		}

		return new MaintainContractBoardAction(StarSystemActorIds.Contracts, tick, additions);
	}

	private bool IsCadenceTick(int tick) =>
		_config.CadenceTicks > 0 && tick % _config.CadenceTicks == 0;

	private int SlotsToFill(StarMap map)
	{
		var target = _config.Placement.TargetGeneratedCount;
		var occupied = map.ContractRegistry.CountGeneratedBoardOccupancy();
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

		return map.ContractRegistry.CountPending() + queued.Count < map.ContractRegistry.MaxPending;
	}

	private static string ContractIdFor(StarMap map, int tick, int slot) =>
		$"contract-gen-{StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("contract-id").Value:x}";

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
			PickDeliveryPickupPoiId(map, tick, slot),
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

	private static string PickDeliveryPickupPoiId(StarMap map, int tick, int slot)
	{
		var candidates = map.PointsOfInterest
			.Where(poi => poi.HasDock && poi.Facilities.Count > 0)
			.Select(poi => poi.Id)
			.Order(StringComparer.Ordinal)
			.ToArray();
		if (candidates.Length == 0)
			throw new InvalidOperationException("Delivery generation requires at least one facility POI.");

		var random = new StableRandom(
			StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("delivery-pickup").Value);
		return candidates[(int)(random.NextDouble() * candidates.Length)];
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
