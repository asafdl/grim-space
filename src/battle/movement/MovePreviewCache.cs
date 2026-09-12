using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// Retains the turn's complete movement tree so queue and undo only select branches.
/// </summary>
public sealed class MovePreviewCache
{
	private readonly Dictionary<(BattleSimulation Sim, string PlayerId), MovePathIndex> _trees = [];

	internal int BuildCount { get; private set; }

	public IReadOnlyList<MovePathSession> GetPaths(
		BattleSimulation sim,
		string playerId,
		IReadOnlyList<IAction> committed)
	{
		var key = (sim, playerId);
		if (!_trees.TryGetValue(key, out var tree))
		{
			tree = MovePathIndex.Build(sim.ForkFromAnchor(), playerId);
			_trees[key] = tree;
			BuildCount++;
		}

		var movementPrefix = committed
			.OfType<MoveStepAction>()
			.Where(action => action.ActorId == playerId)
			.ToArray();
		return tree.GetExtensions(movementPrefix);
	}

	public void Clear()
	{
		_trees.Clear();
		BuildCount = 0;
	}
}
