using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Ids;
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

	private MaintainContractBoardAction BuildMaintainAction(StarMap map, int tick)
	{
		var additions = new List<ContractAddition>();
		if (_generationEnabled() && IsCadenceTick(tick))
		{
			var slots = SlotsToFill(map);
			for (var slot = 0; slot < slots; slot++)
			{
				if (!TryBuildAddition(map, tick, slot, out var addition))
					continue;

				if (!CanRegisterAfterPriorAdditions(map, additions, addition))
					continue;

				additions.Add(addition);
			}
		}

		return new MaintainContractBoardAction(StarSystemActorIds.Contracts, tick, additions);
	}

	// TODO: Cadence is configured as same for contract creation and clear, these are two different flows that need their own timer
	private bool IsCadenceTick(int tick) =>
		_config.CadenceTicks > 0 && tick % _config.CadenceTicks == 0;

	private int SlotsToFill(StarMap map)
	{
		var target = _config.Placement.TargetGeneratedCount;
		var pending = map.ContractRegistry.CountPendingGenerated();
		return System.Math.Max(0, target - pending);
	}

	private bool TryBuildAddition(StarMap map, int tick, int slot, out ContractAddition addition)
	{
		addition = default!;
		var decision = _placement.Pick(map, tick, slot);
		if (decision is null)
			return false;

		var contractId = ContractIdFor(map, tick, slot);
		var danger = ContractDangerProgression.RollDanger(map, tick, slot);
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
				contract = ContractFactory.Build(
					map,
					contractId,
					EContractKind.Hunt,
					BuildHuntArgs(map, decision.IssuerPoiId, danger, tick, slot));
				return true;
			case EContractKind.Delivery:
				contract = ContractFactory.Build(
					map,
					contractId,
					EContractKind.Delivery,
					BuildDeliveryArgs(map, decision.IssuerPoiId, danger, tick, slot));
				return true;
			case EContractKind.Wreckage:
				return ContractFactory.TryBuildWreckage(
					map,
					contractId,
					BuildWreckageArgs(map, decision.IssuerPoiId, danger, tick, slot),
					out contract);
			default:
				throw new ArgumentOutOfRangeException(nameof(decision.Kind), decision.Kind, null);
		}
	}

	private HuntCreateArgs BuildHuntArgs(
		StarMap map,
		string issuerPoiId,
		EDangerLevel danger,
		int tick,
		int slot)
	{
		var landmarkIds = MapLandmarkQueries.AllIds(map);
		var areaPickMix = (long)StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("hunt-area").Value;
		var narrative = _narrativePicker.Pick(map, issuerPoiId, EContractKind.Hunt, tick, slot);

		return new HuntCreateArgs(
			issuerPoiId,
			new AreaPickerArgs(landmarkIds, DeterministicPickMix: areaPickMix),
			danger,
			narrative);
	}

	private DeliveryCreateArgs BuildDeliveryArgs(
		StarMap map,
		string issuerPoiId,
		EDangerLevel danger,
		int tick,
		int slot)
	{
		var narrative = _narrativePicker.Pick(map, issuerPoiId, EContractKind.Delivery, tick, slot);
		return new DeliveryCreateArgs(issuerPoiId, danger, narrative);
	}

	private WreckageCreateArgs BuildWreckageArgs(
		StarMap map,
		string issuerPoiId,
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
		var narrative = _narrativePicker.Pick(map, issuerPoiId, EContractKind.Wreckage, tick, slot);

		return new WreckageCreateArgs(
			issuerPoiId,
			new AreaPickerArgs(
				navLandmarkIds,
				WreckageMinimumPoiClearance,
				DeterministicPickMix: areaPickMix,
				ReferenceMode: referenceMode,
				BorderReferenceConfig: new AreaBorderReferenceConfig()),
			danger,
			narrative);
	}
}
