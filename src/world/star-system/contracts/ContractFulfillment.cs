using GrimSpace.Core.Actions;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractFulfillment
{
	public static bool IsFulfilled(StarMap map, ActiveContract active) =>
		active.Definition.Objective switch
		{
			HuntObjective => AreHuntTargetsEliminated(map, active.State),
			_ => false,
		};

	public static IReadOnlyList<IAction> ReactionsFor(StarMap map, string actorId) =>
		map.ContractRegistry.ActiveFor(actorId)
			.Where(active => IsFulfilled(map, active))
			.Select(active => (IAction)new CompleteContractAction(
				actorId,
				active.Definition.Id,
				active.Definition.Terms.Payment))
			.ToArray();

	private static bool AreHuntTargetsEliminated(StarMap map, ContractState state)
	{
		if (state.SpawnBindings.Count == 0)
			return false;

		foreach (var (_, unitIds) in state.SpawnBindings)
		{
			foreach (var unitId in unitIds)
			{
				if (map.FleetRegistry.Contains(unitId))
					return false;
			}
		}

		return true;
	}
}
