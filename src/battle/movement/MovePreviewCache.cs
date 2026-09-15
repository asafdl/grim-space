using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Movement;

public sealed class MovePreviewCache
{
	private BattleSimulation? _sim;
	private string? _actorId;
	private int _worldVersion;
	private IReadOnlyList<IAction> _actions = [];
	private IReadOnlyList<MovePathSession> _paths = [];

	internal int BuildCount { get; private set; }

	public IReadOnlyList<MovePathSession> GetPaths(BattleSimulation sim, string actorId)
	{
		if (ReferenceEquals(_sim, sim)
			&& _actorId == actorId
			&& _worldVersion == sim.WorldVersion
			&& _actions.SequenceEqual(sim.Actions))
			return _paths;

		_sim = sim;
		_actorId = actorId;
		_worldVersion = sim.WorldVersion;
		_actions = sim.Actions.ToArray();
		_paths = MovePathEndpoints.DiscoverExtensions(sim, actorId);
		BuildCount++;
		return _paths;
	}

	public void Clear()
	{
		_sim = null;
		_actorId = null;
		_worldVersion = 0;
		_actions = [];
		_paths = [];
		BuildCount = 0;
	}
}
