using GrimSpace.Core.Actions;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.World.StarSystem.Objectives;

public static class StoryObjectiveFulfillment
{
	public static IReadOnlyList<IAction> ReactionsFor(
		StarMap map,
		string? playerId,
		AcceptContractAction accepted)
	{
		if (playerId is null || accepted.ActorId != playerId)
			return [];

		return map.StoryObjectives.Active
			.Where(objective =>
				objective.Id == StoryObjective.FirstContractId
				|| objective.RequiredContractId == accepted.ContractId)
			.Select(objective => (IAction)new CompleteStoryObjectiveAction(
				accepted.ActorId,
				objective.Id))
			.ToArray();
	}
}
