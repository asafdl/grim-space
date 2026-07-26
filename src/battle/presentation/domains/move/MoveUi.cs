using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

public static class MoveUi
{
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

	public static IReadOnlyList<Option> GetMoveOptions(BattleOrchestrator battle, Unit? actor)
	{
		if (actor is null || !battle.CanAct(actor))
			return [];

		return GetLegalMoves(battle);
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

	private static IReadOnlyList<Option> GetLegalMoves(BattleOrchestrator battle)
	{
		var actorId = battle.PlayerId;
		var sim = battle.Sim;
		if (sim.RuntimeFor(actorId).IsMovePathStarted)
			return [];

		var origin = sim.StateOf<ActorState>(actorId).Position;
		var frame = BodyFrame.From(sim.StateOf<ActorState>(actorId));
		var startCount = sim.Actions.Count;
		var results = new Dictionary<Coord, Option>();

		foreach (var searchFrame in sim.Search(actorId, [MoveDef.Instance], BattleSearchVisit.MoveVisit))
		{
			var steps = searchFrame.Actions
				.Skip(startCount)
				.OfType<MoveStepAction>()
				.Where(step => step.ActorId == actorId)
				.ToList();

			if (steps.Count == 0)
				continue;

			var runtime = searchFrame.Runtimes.For(actorId);
			var option = MovePathRules.ToEndpointOption(origin, frame, steps, runtime);
			if (option is null)
				continue;

			if (!results.TryGetValue(option.EndPosition, out var existing) || option.ApCost < existing.ApCost)
				results[option.EndPosition] = option;
		}

		return results.Values.ToList();
	}
}
