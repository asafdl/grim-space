using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Objectives;

public static class ObjectivesCollector
{
	public static IReadOnlyList<ActiveObjective> Collect(StarMap map, string playerUnitId)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(playerUnitId);

		var objectives = new List<ActiveObjective>();

		foreach (var active in map.ContractRegistry.ActiveFor(playerUnitId))
			objectives.Add(ContractObjectiveProjection.Project(map, active.Definition));

		foreach (var story in map.StoryObjectives.Active)
			objectives.Add(new ActiveObjective(
				story.Id,
				story.Title,
				new ObjectiveSummaryContent.Plain(story.Summary),
				ResourceBundle.Empty,
				EObjectiveSource.Story));

		return objectives;
	}
}
