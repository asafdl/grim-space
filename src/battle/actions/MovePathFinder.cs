using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
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
		ActorSessionSnapshot Session);

	public static IReadOnlyList<Option> Find(
		BattleBoard board,
		ActorSession session,
		string actorId)
	{
		var scratchBoard = board.Fork();
		var actor = scratchBoard.StateOf(actorId);
		var scratchSession = ActorSessionCopy.Clone(session);
		BeginMovePath(scratchSession, actor.MomentumLevel);

		var results = new Dictionary<Coord, Option>();
		var visited = new Dictionary<SearchNode, int>();
		var undoStack = new Stack<SimulationFrame>();

		Search(
			actor.Position,
			actor.ActionPoints,
			scratchBoard,
			scratchSession,
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
		ActorSession session,
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
			session.UsedDirectionsMask,
			actor.MomentumLevel,
			session.MinPathApCost,
			apRemaining);

		if (visited.TryGetValue(node, out var seenAp) && seenAp >= apRemaining)
			return;

		visited[node] = apRemaining;

		var frame = BodyFrame.From(actor);

		foreach (var direction in Directions)
		{
			if (MoveDirectionRules.UsesOpposite(session.UsedDirectionsMask, direction))
				continue;

			var stepCost = StepCosts.GetMoveStepApCost(
				direction,
				new MoveStepContext(session.PathForwardSteps, actor.MomentumLevel));
			if (stepCost > apRemaining)
				continue;

			var next = position + frame.Step(direction);
			var blocked = board.BlockedFor(actorId);
			if (!board.Grid.IsInBounds(next) || blocked.Contains(next))
				continue;

			var step = new MoveStepAction(actorId, direction);
			PushFrame(undoStack, actor, session);
			if (!step.IsLegal(board, session))
			{
				PopFrame(undoStack, actor, session);
				continue;
			}

			((IAction<BattleBoard, ActorSession>)step).Apply(board, session);

			var fullPath = new List<Coord>(pathSoFar) { next };
			var totalAp = session.PathApSpent;

			if (session.MinPathApCost == 0 || totalAp == 0)
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
				session,
				actorId,
				fullPath,
				results,
				visited,
				undoStack);

			PopFrame(undoStack, actor, session);
		}
	}

	private static void PushFrame(
		Stack<SimulationFrame> undoStack,
		State actor,
		ActorSession session) =>
		undoStack.Push(new SimulationFrame(
			actor.Position,
			actor.MomentumLevel,
			actor.ActionPoints,
			actor.Hp,
			ActorSessionCopy.Snapshot(session)));

	private static void PopFrame(
		Stack<SimulationFrame> undoStack,
		State actor,
		ActorSession session)
	{
		var frame = undoStack.Pop();
		actor.Position = frame.Position;
		actor.MomentumLevel = frame.MomentumLevel;
		actor.ActionPoints = frame.ActionPoints;
		actor.Hp = frame.Hp;
		ActorSessionCopy.Restore(session, frame.Session);
	}

	private static void BeginMovePath(ActorSession session, int startMomentum)
	{
		session.MinPathApCost = ActorSession.InitialMinPathApCost;
		session.PathApSpent = 0;
		session.PathForwardSteps = 0;
		session.UsedDirectionsMask = 0;
		session.MoveStartMomentumLevel = startMomentum;
		session.MovementBuildupLevel = startMomentum;
		session.MovementBuildupForwardSteps = 0;
	}
}
