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

	public static void Evaluate(StarMap map, string holderUnitId)
	{
		foreach (var active in map.ContractRegistry.ActiveFor(holderUnitId).ToArray())
		{
			if (!IsFulfilled(map, active))
				continue;

			map.ContractRegistry.Complete(active.Definition.Id);
		}
	}

	private static bool AreHuntTargetsEliminated(StarMap map, ContractState state)
	{
		if (state.SpawnBindings.Count == 0)
			return false;

		foreach (var (_, unitIds) in state.SpawnBindings)
		{
			foreach (var unitId in unitIds)
			{
				if (map.UnitRegistry.Contains(unitId))
					return false;
			}
		}

		return true;
	}
}
