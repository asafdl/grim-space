using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.World.StarSystem.Objectives;

public static class ObjectivesCollector
{
	public static IReadOnlyList<ActiveObjective> Collect(StarMap map, string playerUnitId)
	{
		var objectives = new List<ActiveObjective>();

		foreach (var active in map.ContractRegistry.ActiveFor(playerUnitId))
		{
			var contract = active.Definition;
			objectives.Add(new ActiveObjective(
				contract.Id,
				ContractDisplay.Title(contract),
				ContractDisplay.ObjectivePreview(contract),
				EObjectiveSource.Contract));
		}

		foreach (var story in map.StoryObjectives.Active)
			objectives.Add(new ActiveObjective(
				story.Id,
				story.Title,
				story.Summary,
				EObjectiveSource.Story));

		return objectives;
	}
}
