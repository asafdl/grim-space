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
/// Turn-scoped movement search with lazy per-prefix option extraction.
/// </summary>
public sealed class MoveOptionIndex
{
	private static readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] MovementActionDefs =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];

	private readonly IReadOnlyList<SearchNode> _nodes;
	private readonly Dictionary<string, SearchNode> _nodeByPrefix;
	private readonly Dictionary<string, IReadOnlyList<Option>> _optionsByPrefix = new();

	private MoveOptionIndex(IReadOnlyList<SearchNode> nodes, Dictionary<string, SearchNode> nodeByPrefix)
	{
		_nodes = nodes;
		_nodeByPrefix = nodeByPrefix;
	}

	public int PrefixCount => _nodeByPrefix.Count;

	public readonly record struct SearchNode(
		IReadOnlyList<IAction> Actions,
		Coord Position,
		BodyFrame Frame,
		MovePathSnapshot PathState);

	public static MoveOptionIndex FromSimulation(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId)
	{
		var nodeByPrefix = new Dictionary<string, SearchNode>();
		var nodes = new List<SearchNode>();

		foreach (var frame in sim.Search(actorId, MovementActionDefs, BattleSearchVisit.ForCapabilities))
		{
			var node = Project(frame, actorId);
			nodes.Add(node);
			nodeByPrefix[PrefixKey(node.Actions)] = node;
		}

		return new MoveOptionIndex(nodes, nodeByPrefix);
	}

	public IReadOnlyList<Option> GetOptions(IReadOnlyList<IAction> committed)
	{
		if (committed.Any(IsWeaponAction))
			return [];

		var key = PrefixKey(committed);
		if (_optionsByPrefix.TryGetValue(key, out var cached))
			return cached;

		if (!_nodeByPrefix.TryGetValue(key, out var origin))
			return [];

		var options = ExtractMoveOptions(origin);
		_optionsByPrefix[key] = options;
		return options;
	}

	public bool ContainsPrefix(IReadOnlyList<IAction> committed) =>
		_nodeByPrefix.ContainsKey(PrefixKey(committed));

	private IReadOnlyList<Option> ExtractMoveOptions(SearchNode origin)
	{
		var startCount = origin.Actions.Count;
		var results = new Dictionary<Coord, Option>();

		foreach (var searchNode in _nodes)
		{
			if (searchNode.Actions.Count <= startCount)
				continue;

			if (!PrefixStartsWith(searchNode.Actions, origin.Actions))
				continue;

			var steps = searchNode.Actions
				.Skip(startCount)
				.OfType<MoveStepAction>()
				.ToList();
			if (steps.Count == 0)
				continue;

			var option = MovePathRules.ToEndpointOption(
				origin.Position,
				origin.Frame,
				steps,
				searchNode.PathState,
				origin.PathState.PathApSpent);
			if (option is null)
				continue;

			if (!results.TryGetValue(option.EndPosition, out var existing)
				|| MovePathRules.PreferEndpointOption(option, existing))
				results[option.EndPosition] = option;
		}

		return results.Values.ToList();
	}

	internal static SearchNode Project(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		string actorId)
	{
		var actor = frame.World.StateOf(actorId);
		return new SearchNode(
			frame.Actions,
			actor.Position,
			BodyFrame.From(actor),
			MovePathSnapshot.From(frame.Runtimes.For(actorId)));
	}

	public static string PrefixKey(IReadOnlyList<IAction> actions) =>
		actions.Count == 0
			? string.Empty
			: string.Join('|', actions.Select(ActionKey));

	private static bool PrefixStartsWith(IReadOnlyList<IAction> actions, IReadOnlyList<IAction> prefix)
	{
		if (actions.Count < prefix.Count)
			return false;

		for (var i = 0; i < prefix.Count; i++)
		{
			if (ActionKey(actions[i]) != ActionKey(prefix[i]))
				return false;
		}

		return true;
	}

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
