namespace GrimSpace.World.StarSystem.Objectives;

public sealed class StoryObjectiveRegistry
{
	private readonly List<StoryObjective> _active = [];

	public IReadOnlyList<StoryObjective> Active => _active;

	public void Add(StoryObjective objective)
	{
		ArgumentNullException.ThrowIfNull(objective);
		ArgumentException.ThrowIfNullOrEmpty(objective.Id);

		if (_active.Any(entry => entry.Id == objective.Id))
			throw new InvalidOperationException($"Story objective '{objective.Id}' is already active.");

		_active.Add(objective);
	}

	public bool TryComplete(string objectiveId)
	{
		ArgumentException.ThrowIfNullOrEmpty(objectiveId);
		var removed = _active.RemoveAll(entry => entry.Id == objectiveId);
		return removed > 0;
	}

	public StoryObjectiveRegistry CloneForFork()
	{
		var clone = new StoryObjectiveRegistry();
		clone._active.AddRange(_active);
		return clone;
	}
}
