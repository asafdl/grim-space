using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contact;

public readonly record struct PendingWreckDecision(
	string ContractId,
	Contract Contract);

public static class WreckageQueries
{
	public static bool TryGetPendingPlayerWreckDecision(
		StarMap world,
		string playerId,
		out PendingWreckDecision decision)
	{
		decision = default;
		if (!world.FleetRegistry.TryGet(playerId, out var player))
			return false;

		var contractId = player.State.PendingWreckContractId;
		if (string.IsNullOrEmpty(contractId))
			return false;

		if (!world.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not WreckageObjective wreckage)
			return false;

		decision = new PendingWreckDecision(contractId, contract);
		return true;
	}

	/// <summary>Ambush fleets carry their wreck contract as <c>SourceContractId</c>.</summary>
	public static bool TryGetAmbushContract(StarMap world, string fleetId, out string contractId)
	{
		contractId = "";
		if (!world.FleetRegistry.TryGet(fleetId, out var fleet)
			|| fleet.State.SourceContractId is not { } sourceId
			|| !world.ContractRegistry.TryGetState(sourceId, out var state)
			|| state is not WreckageContractState { Status: EContractStatus.Active, RequiresAmbushResolution: true })
			return false;

		contractId = sourceId;
		return true;
	}

	internal static bool IsActiveWreckContractForHolder(
		StarMap world,
		string actorId,
		string contractId) =>
		world.ContractRegistry.TryGet(contractId, out var contract)
		&& world.ContractRegistry.TryGetState(contractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state.HolderUnitId == actorId
		&& state is WreckageContractState wreckage
		&& !wreckage.IsObjectiveMet()
		&& contract.Objective is WreckageObjective;
}
