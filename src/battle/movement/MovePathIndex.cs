using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// One movement search tree with lazy endpoint extraction for each queued branch.
/// </summary>
public sealed class MovePathIndex
{
	private const int MaxDepth = 12;

	private readonly string _actorId;
	private readonly SearchNode _root;

	private MovePathIndex(string actorId, SearchNode root)
	{
		_actorId = actorId;
		_root = root;
	}

	public static MovePathIndex Build(BattleSimulation sim, string actorId)
	{
		var fork = sim.Fork();
		return new MovePathIndex(actorId, BuildNode(fork, actorId, depth: 0));
	}

	public IReadOnlyList<MovePathSession> GetExtensions(IReadOnlyList<MoveStepAction> prefix)
	{
		if (!TryLocate(prefix, out var origin))
			return [];
		if (origin.Extensions is not null)
			return origin.Extensions;

		var start = new MoveCheckpoint(
			origin.ResultState.Position,
			GridBasis.From(
				origin.ResultState.Fore,
				origin.ResultState.Dorsal,
				origin.ResultState.Starboard));
		var results = new Dictionary<(Coord Position, GridBasis Basis), MovePathSession>();
		var steps = new List<MoveStepAction>();

		CollectExtensions(origin, start, steps, results);

		origin.Extensions = results.Values
			.OrderBy(path => path.EndPosition.X)
			.ThenBy(path => path.EndPosition.Y)
			.ThenBy(path => path.EndPosition.Z)
			.ThenBy(path => path, MovePathRankComparer.Instance)
			.ThenBy(path => path.EndBasis.Forward.X)
			.ThenBy(path => path.EndBasis.Forward.Y)
			.ThenBy(path => path.EndBasis.Forward.Z)
			.ThenBy(path => path.EndBasis.Up.X)
			.ThenBy(path => path.EndBasis.Up.Y)
			.ThenBy(path => path.EndBasis.Up.Z)
			.ToList();
		return origin.Extensions;
	}

	public bool ContainsPrefix(IReadOnlyList<MoveStepAction> prefix) =>
		TryLocate(prefix, out _);

	private static SearchNode BuildNode(BattleSimulation sim, string actorId, int depth)
	{
		var node = new SearchNode(sim.StateOf<State>(actorId).Clone());
		if (!node.ResultState.IsAlive || depth >= MaxDepth)
			return node;

		var candidates = MoveDef.Instance
			.Discover(sim.World, sim.RuntimeFor(actorId), actorId)
			.Cast<MoveStepAction>()
			.ToArray();
		foreach (var action in candidates)
		{
			var checkpoint = sim.Actions.Count;
			if (!sim.TryEnqueue(action))
				continue;

			if (sim.InvariantStatus != InvariantStatus.Impossible)
			{
				var child = BuildNode(sim, actorId, depth + 1);
				if (child.ResultState.IsAlive)
					node.Children.Add(new SearchEdge(action, child));
			}
			sim.Dequeue(checkpoint);
		}

		return node;
	}

	private void CollectExtensions(
		SearchNode node,
		MoveCheckpoint start,
		List<MoveStepAction> steps,
		Dictionary<(Coord Position, GridBasis Basis), MovePathSession> results)
	{
		foreach (var edge in node.Children)
		{
			steps.Add(edge.Action);
			var extension = steps.ToArray();
			var path = new MovePathSession(
				_actorId,
				extension,
				BuildCheckpoints(start, extension),
				edge.Child.ResultState.ActionPoints,
				edge.Child.ResultState.Clone());
			var end = (path.EndPosition, path.EndBasis);
			if (!results.TryGetValue(end, out var existing) || Compare(path, existing) < 0)
				results[end] = path;

			CollectExtensions(edge.Child, start, steps, results);
			steps.RemoveAt(steps.Count - 1);
		}
	}

	private bool TryLocate(IReadOnlyList<MoveStepAction> prefix, out SearchNode node)
	{
		node = _root;
		foreach (var action in prefix)
		{
			var edge = node.Children.FirstOrDefault(candidate => candidate.Action == action);
			if (edge is null)
				return false;
			node = edge.Child;
		}

		return true;
	}

	private static IReadOnlyList<MoveCheckpoint> BuildCheckpoints(
		MoveCheckpoint start,
		IReadOnlyList<MoveStepAction> steps)
	{
		var checkpoints = new List<MoveCheckpoint>(steps.Count + 1) { start };
		var position = start.Position;
		var basis = start.Basis;

		foreach (var step in steps)
		{
			var transition = Orientation.MoveStep(position, basis, step.Heading, step.Roll);
			position = transition.Destination;
			basis = transition.ArrivalBasis;
			checkpoints.Add(new MoveCheckpoint(position, basis));
		}

		return checkpoints;
	}

	private static int Compare(MovePathSession left, MovePathSession right) =>
		MovePathRankComparer.Instance.Compare(left, right);

	private sealed class SearchNode(State resultState)
	{
		public State ResultState { get; } = resultState;
		public List<SearchEdge> Children { get; } = [];
		public IReadOnlyList<MovePathSession>? Extensions { get; set; }
	}

	private sealed record SearchEdge(MoveStepAction Action, SearchNode Child);

	private sealed class MovePathRankComparer : IComparer<MovePathSession>
	{
		public static MovePathRankComparer Instance { get; } = new();

		public int Compare(MovePathSession? left, MovePathSession? right)
		{
			if (ReferenceEquals(left, right))
				return 0;
			if (left is null)
				return 1;
			if (right is null)
				return -1;

			var comparison = left.Steps.Count.CompareTo(right.Steps.Count);
			if (comparison != 0)
				return comparison;

			comparison = left.Steps.Count(step => step.Heading is not null)
				.CompareTo(right.Steps.Count(step => step.Heading is not null));
			if (comparison != 0)
				return comparison;

			comparison = left.Steps.Count(step => step.Roll is not null)
				.CompareTo(right.Steps.Count(step => step.Roll is not null));
			if (comparison != 0)
				return comparison;

			for (var i = 0; i < left.Steps.Count; i++)
			{
				comparison = HeadingOrder(left.Steps[i].Heading).CompareTo(HeadingOrder(right.Steps[i].Heading));
				if (comparison != 0)
					return comparison;

				comparison = RollOrder(left.Steps[i].Roll).CompareTo(RollOrder(right.Steps[i].Roll));
				if (comparison != 0)
					return comparison;
			}

			return 0;
		}

		private static int HeadingOrder(Movement.Enums.EHeadingTurn? heading) =>
			heading switch
			{
				null => 0,
				Movement.Enums.EHeadingTurn.YawLeft => 1,
				Movement.Enums.EHeadingTurn.YawRight => 2,
				Movement.Enums.EHeadingTurn.PitchUp => 3,
				Movement.Enums.EHeadingTurn.PitchDown => 4,
				_ => int.MaxValue,
			};

		private static int RollOrder(Movement.Enums.ERollDirection? roll) =>
			roll switch
			{
				null => 0,
				Movement.Enums.ERollDirection.Clockwise => 1,
				Movement.Enums.ERollDirection.CounterClockwise => 2,
				_ => int.MaxValue,
			};
	}
}
