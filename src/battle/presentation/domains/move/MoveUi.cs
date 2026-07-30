using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

/// <summary>
/// Turn-scoped movement preview: search cache, option lookup by committed queue, and apply helpers.
/// </summary>
public sealed class MoveUi
{
	private static readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] MovementActionDefs =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];

	private readonly string _actorId;
	private readonly IReadOnlyList<SearchFrame<BattleWorld, ActorRuntime>> _frames;
	private readonly IReadOnlyDictionary<string, SearchFrame<BattleWorld, ActorRuntime>> _nodesByPrefix;

	private MoveUi(
		string actorId,
		IReadOnlyList<SearchFrame<BattleWorld, ActorRuntime>> frames,
		IReadOnlyDictionary<string, SearchFrame<BattleWorld, ActorRuntime>> nodesByPrefix)
	{
		_actorId = actorId;
		_frames = frames;
		_nodesByPrefix = nodesByPrefix;
	}

	public static MoveUi Build(BattleOrchestrator battle)
	{
		var actorId = battle.PlayerId;
		var frames = battle.Sim
			.Search(actorId, MovementActionDefs, BattleSearchVisit.ForCapabilities)
			.ToList();

		var nodesByPrefix = new Dictionary<string, SearchFrame<BattleWorld, ActorRuntime>>();
		foreach (var frame in frames)
			nodesByPrefix[PrefixKey(frame.Actions)] = frame;

		return new MoveUi(actorId, frames, nodesByPrefix);
	}

	public IReadOnlyList<Option> GetMoveOptions(IReadOnlyList<IAction> committed) =>
		_nodesByPrefix.TryGetValue(PrefixKey(committed), out var node)
			? ExtractMoveOptions(committed.Count, node)
			: [];

	public bool TryLocate(
		IReadOnlyList<IAction> committed,
		out SearchFrame<BattleWorld, ActorRuntime> node) =>
		_nodesByPrefix.TryGetValue(PrefixKey(committed), out node);

	public static IReadOnlyList<Option> GetMoveOptions(BattleOrchestrator battle, Unit? actor)
	{
		if (actor is null || !battle.CanAct(actor))
			return [];

		return battle.MoveUi.GetMoveOptions(battle.Sim.Actions);
	}

	public static IReadOnlyList<MoveStepAction>? Translate(
		BattleSimulation sim,
		string actorId,
		Option option)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		try
		{
			return MoveDef.StepsFromPath(actorId, BodyFrame.From(actor), actor.Position, option.Path);
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	public static bool TryApply(BattleOrchestrator battle, Interaction.InteractionState state, Option option)
	{
		var actor = battle.GetActiveActor();
		if (actor is null || !battle.CanAct(actor))
			return false;

		var steps = Translate(battle.Sim, battle.PlayerId, option);
		if (steps is null || !battle.Sim.TryEnqueue(actions: [..steps]))
			return false;

		state.CommittedMovePath = option.Path;
		state.ClearInteraction();
		return true;
	}

	public static (IReadOnlyList<Coord> Path, Coord? Target) GetPathHighlights(
		IReadOnlyList<Option> options,
		int? hoveredIndex,
		IReadOnlyList<Coord> committedPath)
	{
		if (hoveredIndex is int i)
			return (options[i].Path, options[i].EndPosition);

		if (committedPath.Count > 0)
			return (committedPath, committedPath[^1]);

		return ([], null);
	}

	private IReadOnlyList<Option> ExtractMoveOptions(
		int startCount,
		SearchFrame<BattleWorld, ActorRuntime> originNode)
	{
		if (originNode.Runtimes.For(_actorId).IsMovePathStarted)
			return [];

		var actor = originNode.World.StateOf(_actorId);
		var origin = actor.Position;
		var frame = BodyFrame.From(actor);
		var committed = originNode.Actions;
		var results = new Dictionary<Coord, Option>();

		foreach (var searchFrame in _frames)
		{
			if (searchFrame.Actions.Count <= startCount)
				continue;

			if (!PrefixStartsWith(searchFrame.Actions, committed))
				continue;

			var steps = searchFrame.Actions
				.Skip(startCount)
				.OfType<MoveStepAction>()
				.Where(step => step.ActorId == _actorId)
				.ToList();
			if (steps.Count == 0)
				continue;

			var runtime = searchFrame.Runtimes.For(_actorId);
			var option = MovePathRules.ToEndpointOption(origin, frame, steps, runtime);
			if (option is null)
				continue;

			if (!results.TryGetValue(option.EndPosition, out var existing) || option.ApCost < existing.ApCost)
				results[option.EndPosition] = option;
		}

		return results.Values.ToList();
	}

	private static string PrefixKey(IReadOnlyList<IAction> actions) =>
		actions.Count == 0
			? string.Empty
			: string.Join('|', actions.Select(ActionKey));

	internal static string PrefixKeyForTests(IReadOnlyList<IAction> actions) => PrefixKey(actions);

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
			_ => action.GetType().FullName ?? "action",
		};
}
