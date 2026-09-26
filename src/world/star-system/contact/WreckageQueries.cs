using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contact;

public readonly record struct PendingWreckDecision(
	string ContractId,
	string Title,
	string Briefing,
	bool IsAmbush);

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

		decision = new PendingWreckDecision(
			contractId,
			contract.Narrative.Title,
			contract.Narrative.Briefing,
			wreckage.Outcome is WreckageOutcome.Ambush);
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
		&& !ContractFactory.IsWreckageObjectiveMet(contractId, world, actorId)
		&& contract.Objective is WreckageObjective;
}
