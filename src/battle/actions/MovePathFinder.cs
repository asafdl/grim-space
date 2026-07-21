using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public static class MovePathFinder
{
	private static readonly EStepDirection[] Directions = Enum.GetValues<EStepDirection>();

	private readonly record struct SearchNode(
		Coord Position,
		int UsedDirectionsMask,
		int MomentumLevel,
		int MinPathApCost,
		int ApRemaining);

	private readonly record struct SimulationFrame(
		Coord Position,
		int MomentumLevel,
		int ActionPoints,
		int Hp,
		TurnStateSnapshot TurnState);

	public static IReadOnlyList<Option> Find(
		BattleBoard board,
		BattlePlanContext context,
		string actorId)
	{
		var scratchTimeline = new Timeline();
		var scratchBoard = board.Clone();
		var actor = scratchBoard.StateOf(actorId);
		var turnState = context.TurnState.Clone();
		turnState.ResetMovePath(actor.MomentumLevel);

		var simContext = new BattlePlanContext([], turnState);
		var results = new Dictionary<Coord, Option>();
		var visited = new Dictionary<SearchNode, int>();
		var undoStack = new Stack<SimulationFrame>();

		Search(
			actor.Position,
			actor.ActionPoints,
			scratchBoard,
			simContext,
			scratchTimeline,
			actorId,
			[],
			results,
			visited,
			undoStack);

		return results.Values.ToList();
	}

	private static void Search(
		Coord position,
		int apRemaining,
		BattleBoard board,
		BattlePlanContext context,
		Timeline timeline,
		string actorId,
		List<Coord> pathSoFar,
		Dictionary<Coord, Option> results,
		Dictionary<SearchNode, int> visited,
		Stack<SimulationFrame> undoStack)
	{
		if (apRemaining <= 0)
			return;

		var turnState = context.TurnState;
		var actor = board.StateOf(actorId);
		var node = new SearchNode(
			position,
			turnState.UsedDirectionsMask,
			actor.MomentumLevel,
			turnState.MinPathApCost,
			apRemaining);

		if (visited.TryGetValue(node, out var seenAp) && seenAp >= apRemaining)
			return;

		visited[node] = apRemaining;

		var frame = BodyFrame.From(actor);

		foreach (var direction in Directions)
		{
			if (MoveDirectionRules.UsesOpposite(turnState.UsedDirectionsMask, direction))
				continue;

			var stepCost = StepCosts.GetMoveStepApCost(
				direction,
				new MoveStepContext(turnState.PathForwardSteps, actor.MomentumLevel));
			if (stepCost > apRemaining)
				continue;

			var next = position + frame.Step(direction);
			var blocked = board.BlockedFor(actorId);
			if (!board.Grid.IsInBounds(next) || blocked.Contains(next))
				continue;

			var step = new MoveStepAction(actorId, position, next, turnState.UsedDirectionsMask);
			PushFrame(undoStack, actor, turnState);
			if (!ActionApplicator.TryApplyOne(step, board, context, timeline, actorId))
			{
				PopFrame(undoStack, actor, turnState);
				continue;
			}

			var fullPath = new List<Coord>(pathSoFar) { next };
			var totalAp = turnState.PathApSpent;

			if (turnState.MinPathApCost == 0 || totalAp == 0)
			{
				if (!results.TryGetValue(next, out var existing) || totalAp < existing.ApCost)
				{
					results[next] = new Option
					{
						ApCost = totalAp,
						Path = fullPath,
					};
				}
			}

			Search(
				next,
				apRemaining - stepCost,
				board,
				context,
				timeline,
				actorId,
				fullPath,
				results,
				visited,
				undoStack);

			PopFrame(undoStack, actor, turnState);
		}
	}

	private static void PushFrame(
		Stack<SimulationFrame> undoStack,
		State actor,
		TurnState turnState) =>
		undoStack.Push(new SimulationFrame(
			actor.Position,
			actor.MomentumLevel,
			actor.ActionPoints,
			actor.Hp,
			turnState.Snapshot()));

	private static void PopFrame(
		Stack<SimulationFrame> undoStack,
		State actor,
		TurnState turnState)
	{
		var frame = undoStack.Pop();
		actor.Position = frame.Position;
		actor.MomentumLevel = frame.MomentumLevel;
		actor.ActionPoints = frame.ActionPoints;
		actor.Hp = frame.Hp;
		turnState.Restore(frame.TurnState);
	}
}
