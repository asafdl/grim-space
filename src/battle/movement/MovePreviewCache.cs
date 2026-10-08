using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Movement;

public sealed class MovePreviewCache
{
	private readonly List<Entry> _entries = [];

	internal int BuildCount { get; private set; }

	public IReadOnlyList<RouteHitPreview> GetPaths(BattleSimulation sim, string actorId)
	{
		var movementPrefix = MovementPrefix(sim.Actions, actorId);
		var cached = _entries.FirstOrDefault(entry =>
			ReferenceEquals(entry.Sim, sim)
			&& entry.ActorId == actorId
			&& entry.MovementPrefix.SequenceEqual(movementPrefix));
		if (cached is not null)
			return cached.Paths;

		var paths = MovePathEndpoints.DiscoverExtensions(sim, actorId)
			.Select(session => new RouteHitPreview(session))
			.ToList();
		_entries.Add(new Entry(
			sim,
			actorId,
			movementPrefix,
			paths));
		BuildCount++;
		return paths;
	}

	public void Clear()
	{
		_entries.Clear();
		BuildCount = 0;
	}

	internal static IAction[] MovementPrefix(IReadOnlyList<IAction> actions, string actorId) =>
		actions
			.Where(action => action.ActorId == actorId && MovePathIndex.IsMovementAction(action))
			.ToArray();

	private sealed record Entry(
		BattleSimulation Sim,
		string ActorId,
		IReadOnlyList<IAction> MovementPrefix,
		IReadOnlyList<RouteHitPreview> Paths);
}
