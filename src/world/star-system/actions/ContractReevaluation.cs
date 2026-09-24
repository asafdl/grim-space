using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public static class ContractReevaluation
{
	public static IReadOnlyList<CompleteContractAction> ReevaluateFor(
		StarMap map,
		ActorRuntime runtime,
		string actorId,
		EContractKind? kind = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);
		return map.ContractRegistry.ActiveFor(actorId)
			.Where(active => MatchesKind(active.Definition, kind))
			.Select(active => new CompleteContractAction(
				actorId,
				active.Definition.Id,
				active.Definition.Terms.Payment))
			.Where(action => CompleteContractDef.Instance.IsLegal(action, map, runtime))
			.ToArray();
	}

	private static bool MatchesKind(Contract contract, EContractKind? kind) =>
		kind switch
		{
			null => true,
			EContractKind.Hunt => contract.Objective is HuntObjective,
			EContractKind.Delivery => contract.Objective is DeliveryObjective,
			EContractKind.Wreckage => contract.Objective is WreckageObjective,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};
}
