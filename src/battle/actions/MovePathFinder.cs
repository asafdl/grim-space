using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
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
		TurnPhaseContextSnapshot PhaseContext);

	public static IReadOnlyList<Option> Find(
		BattleBoard board,
		TurnPhaseContext phaseContext,
		string actorId)
	{
		var scratchBoard = board.Fork();
		var actor = scratchBoard.StateOf(actorId);
		var scratchPhaseContext = TurnPhaseContextCopy.Clone(phaseContext);
		BeginMovePath(scratchPhaseContext, actor.MomentumLevel);

		var results = new Dictionary<Coord, Option>();
		var visited = new Dictionary<SearchNode, int>();
		var undoStack = new Stack<SimulationFrame>();

		Search(
			actor.Position,
			actor.ActionPoints,
			scratchBoard,
			scratchPhaseContext,
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
		TurnPhaseContext phaseContext,
		string actorId,
		List<Coord> pathSoFar,
		Dictionary<Coord, Option> results,
		Dictionary<SearchNode, int> visited,
		Stack<SimulationFrame> undoStack)
	{
		if (apRemaining <= 0)
			return;

		var actor = board.StateOf(actorId);
		var node = new SearchNode(
			position,
			phaseContext.UsedDirectionsMask,
			actor.MomentumLevel,
			phaseContext.MinPathApCost,
			apRemaining);

		if (visited.TryGetValue(node, out var seenAp) && seenAp >= apRemaining)
			return;

		visited[node] = apRemaining;

		var frame = BodyFrame.From(actor);

		foreach (var direction in Directions)
		{
			if (MoveDirectionRules.UsesOpposite(phaseContext.UsedDirectionsMask, direction))
				continue;

			var stepCost = StepCosts.GetMoveStepApCost(
				direction,
				new MoveStepContext(phaseContext.PathForwardSteps, actor.MomentumLevel));
			if (stepCost > apRemaining)
				continue;

			var next = position + frame.Step(direction);
			var blocked = board.BlockedFor(actorId);
			if (!board.Grid.IsInBounds(next) || blocked.Contains(next))
				continue;

			var step = new MoveStepAction(actorId, direction);
			PushFrame(undoStack, actor, phaseContext);
			var ctx = BattleActionContext.For(board, phaseContext, actorId);
			if (!BattleActionRunner.TryApply(step, ctx))
			{
				PopFrame(undoStack, actor, phaseContext);
				continue;
			}

			var fullPath = new List<Coord>(pathSoFar) { next };
			var totalAp = phaseContext.PathApSpent;

			if (phaseContext.MinPathApCost == 0 || totalAp == 0)
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
				phaseContext,
				actorId,
				fullPath,
				results,
				visited,
				undoStack);

			PopFrame(undoStack, actor, phaseContext);
		}
	}

	private static void PushFrame(
		Stack<SimulationFrame> undoStack,
		State actor,
		TurnPhaseContext phaseContext) =>
		undoStack.Push(new SimulationFrame(
			actor.Position,
			actor.MomentumLevel,
			actor.ActionPoints,
			actor.Hp,
			TurnPhaseContextCopy.Snapshot(phaseContext)));

	private static void PopFrame(
		Stack<SimulationFrame> undoStack,
		State actor,
		TurnPhaseContext phaseContext)
	{
		var frame = undoStack.Pop();
		actor.Position = frame.Position;
		actor.MomentumLevel = frame.MomentumLevel;
		actor.ActionPoints = frame.ActionPoints;
		actor.Hp = frame.Hp;
		TurnPhaseContextCopy.Restore(phaseContext, frame.PhaseContext);
	}

	private static void BeginMovePath(TurnPhaseContext phaseContext, int startMomentum)
	{
		phaseContext.MinPathApCost = TurnPhaseContext.InitialMinPathApCost;
		phaseContext.PathApSpent = 0;
		phaseContext.PathForwardSteps = 0;
		phaseContext.UsedDirectionsMask = 0;
		phaseContext.MoveStartMomentumLevel = startMomentum;
		phaseContext.MovementBuildupLevel = startMomentum;
		phaseContext.MovementBuildupForwardSteps = 0;
	}
}
