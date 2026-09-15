using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Movement;

public sealed class MovePreviewCache
{
	private readonly List<Entry> _entries = [];

	internal int BuildCount { get; private set; }

	public IReadOnlyList<MovePathSession> GetPaths(BattleSimulation sim, string actorId)
	{
		var cached = _entries.FirstOrDefault(entry =>
			ReferenceEquals(entry.Sim, sim)
			&& entry.ActorId == actorId
			&& entry.WorldVersion == sim.WorldVersion
			&& entry.Actions.SequenceEqual(sim.Actions));
		if (cached is not null)
			return cached.Paths;

		var paths = MovePathEndpoints.DiscoverExtensions(sim, actorId);
		_entries.Add(new Entry(
			sim,
			actorId,
			sim.WorldVersion,
			sim.Actions.ToArray(),
			paths));
		BuildCount++;
		return paths;
	}

	public void Clear()
	{
		_entries.Clear();
		BuildCount = 0;
	}

	private sealed record Entry(
		BattleSimulation Sim,
		string ActorId,
		int WorldVersion,
		IReadOnlyList<IAction> Actions,
		IReadOnlyList<MovePathSession> Paths);
}
