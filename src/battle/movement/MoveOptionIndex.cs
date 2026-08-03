using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// Turn-scoped movement search for prefix validation, with live move-only path discovery per prefix.
/// </summary>
public sealed class MoveOptionIndex
{
	private static readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] MovementActionDefs =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];

	private readonly Dictionary<string, SearchNode> _nodeByPrefix;
	private readonly Dictionary<string, IReadOnlyList<MovePathSession>> _pathsByPrefix = new();
	private readonly Simulation<BattleWorld, ActorRuntime> _turnStartSim;
	private readonly string _actorId;

	private MoveOptionIndex(
		Dictionary<string, SearchNode> nodeByPrefix,
		Simulation<BattleWorld, ActorRuntime> turnStartSim,
		string actorId)
	{
		_nodeByPrefix = nodeByPrefix;
		_turnStartSim = turnStartSim;
		_actorId = actorId;
	}

	public int PrefixCount => _nodeByPrefix.Count;

	public readonly record struct SearchNode(
		IReadOnlyList<IAction> Actions,
		Coord Position,
		BodyFrame Frame,
		MovePathSession? ActivePath);

	public static MoveOptionIndex FromSimulation(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId)
	{
		var nodeByPrefix = new Dictionary<string, SearchNode>();

		foreach (var frame in sim.Search(actorId, MovementActionDefs, BattleSearchVisit.ForCapabilities))
		{
			var node = Project(frame, actorId);
			nodeByPrefix[PrefixKey(node.Actions)] = node;
		}

		return new MoveOptionIndex(nodeByPrefix, sim.ForkFromAnchor(), actorId);
	}

	public IReadOnlyList<MovePathSession> GetPaths(IReadOnlyList<IAction> committed)
	{
		if (committed.Any(IsWeaponAction))
			return [];

		var key = PrefixKey(committed);
		if (_pathsByPrefix.TryGetValue(key, out var cached))
			return cached;

		if (committed.Count > 0 && !_nodeByPrefix.ContainsKey(key))
			return [];

		var paths = MovePathDiscovery.DiscoverExtensions(_turnStartSim, _actorId, committed);
		_pathsByPrefix[key] = paths;
		return paths;
	}

	public bool ContainsPrefix(IReadOnlyList<IAction> committed) =>
		_nodeByPrefix.ContainsKey(PrefixKey(committed));

	/// <summary>All action prefixes materialized in the turn-start search tree.</summary>
	internal IEnumerable<IReadOnlyList<IAction>> EnumeratePrefixes() =>
		_nodeByPrefix.Values.Select(node => node.Actions);

	internal static SearchNode Project(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		string actorId)
	{
		var actor = frame.World.StateOf(actorId);
		var activePath = frame.Runtimes.For(actorId).ActivePath?.Clone();
		return new SearchNode(
			frame.Actions,
			actor.Position,
			BodyFrame.From(actor),
			activePath);
	}

	public static string PrefixKey(IReadOnlyList<IAction> actions) =>
		actions.Count == 0
			? string.Empty
			: string.Join('|', actions.Select(ActionKey));

	private static string ActionKey(IAction action) =>
		action switch
		{
			MoveStepAction move => $"move:{move.ActorId}:{move.Direction}",
			HeadingTurnAction heading => $"heading:{heading.ActorId}:{heading.Turn}",
			RollAction roll => $"roll:{roll.ActorId}:{roll.Direction}",
			FlakAction flak => $"flak:{flak.ActorId}:{flak.Mount}",
			RailgunAction railgun => $"railgun:{railgun.ActorId}",
			_ => action.GetType().FullName ?? "action",
		};

	private static bool IsWeaponAction(IAction action) =>
		action is FlakAction or RailgunAction;
}
