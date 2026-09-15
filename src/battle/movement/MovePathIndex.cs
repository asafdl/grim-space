using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public sealed class MovePathIndex
{
	private readonly BattleSimulation _sim;
	private readonly string _actorId;
	private readonly int _startDepth;
	private readonly IEnumerator<SearchFrame<BattleWorld, ActorRuntime>> _frames;
	private readonly List<MovePathSession> _paths = [];
	private readonly List<(IAction[] Prefix, IReadOnlyList<MovePathSession> Paths)> _extensions = [];
	private SearchFrame<BattleWorld, ActorRuntime>? _pendingRemaining;

	private MovePathIndex(
		BattleSimulation sim,
		string actorId,
		int startDepth,
		IEnumerator<SearchFrame<BattleWorld, ActorRuntime>> frames)
	{
		_sim = sim;
		_actorId = actorId;
		_startDepth = startDepth;
		_frames = frames;
	}

	public static MovePathIndex Build(BattleSimulation sim, string actorId)
	{
		var index = Start(sim, actorId);
		index.Complete();
		return index;
	}

	public static MovePathIndex Start(BattleSimulation sim, string actorId)
	{
		var startDepth = sim.Actions.Count;
		var frames = ActionSearch.Run(
			sim,
			actorId,
			Capabilities.Movement,
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForMovePreview,
				actions => actions.Skip(startDepth).All(action => action is MoveStepAction)));
		return new MovePathIndex(sim, actorId, startDepth, frames.GetEnumerator());
	}

	public bool IsPriorityComplete { get; private set; }
	public bool IsComplete { get; private set; }

	public void CompletePriority()
	{
		if (IsPriorityComplete)
			return;

		while (_frames.MoveNext())
		{
			if (_frames.Current.Phase == SearchFramePhase.Remaining)
			{
				_pendingRemaining = _frames.Current;
				IsPriorityComplete = true;
				return;
			}

			Add(_frames.Current);
		}

		IsPriorityComplete = true;
		IsComplete = true;
		_frames.Dispose();
	}

	public bool AdvanceRemaining()
	{
		CompletePriority();
		if (IsComplete)
			return false;

		if (_pendingRemaining is { } pending)
		{
			_pendingRemaining = null;
			Add(pending);
			return true;
		}

		if (_frames.MoveNext())
		{
			Add(_frames.Current);
			return true;
		}

		IsComplete = true;
		_frames.Dispose();
		return false;
	}

	public void Complete()
	{
		CompletePriority();
		while (AdvanceRemaining()) { }
	}

	public IReadOnlyList<MovePathSession> GetExtensions(IReadOnlyList<IAction> prefix)
	{
		var cached = _extensions.FirstOrDefault(entry => entry.Prefix.SequenceEqual(prefix));
		if (cached.Paths is not null)
			return cached.Paths;

		var prefixMoves = prefix.Count(action => action is MoveStepAction);
		var results = new Dictionary<(Coord Position, GridBasis Basis), MovePathSession>();
		foreach (var path in _paths)
		{
			if (path.Steps.Count <= prefix.Count || !StartsWith(path.Steps, prefix))
				continue;

			var extension = new MovePathSession(
				path.ActorId,
				path.Steps.Skip(prefix.Count).ToArray(),
				path.Checkpoints.Skip(prefixMoves).ToArray(),
				path.RemainingAp,
				path.ResultState.Clone());
			var end = (extension.EndPosition, extension.EndBasis);
			if (!results.TryGetValue(end, out var existing)
				|| MovePathRankComparer.Instance.Compare(extension, existing) < 0)
				results[end] = extension;
		}

		var paths = results.Values
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
		_extensions.Add((prefix.ToArray(), paths));
		return paths;
	}

	private void Add(SearchFrame<BattleWorld, ActorRuntime> frame)
	{
		var steps = frame.Actions.Skip(_startDepth).ToArray();
		if (steps.Length == 0 || steps.Any(action => !IsMovementAction(action)))
			return;

		var result = frame.World.StateOf(_actorId).Clone();
		_paths.Add(new MovePathSession(
			_actorId,
			steps,
			ProjectCheckpoints(_sim, _actorId, steps, includeStart: true),
			result.ActionPoints,
			result));
		_extensions.Clear();
	}

	internal static IReadOnlyList<MoveCheckpoint> ProjectCheckpoints(
		BattleSimulation sim,
		string actorId,
		IReadOnlyList<IAction> actions,
		bool includeStart)
	{
		var projection = sim.Fork();
		var checkpoints = new List<MoveCheckpoint>();
		if (includeStart)
			checkpoints.Add(CaptureCheckpoint(projection.StateOf<State>(actorId)));

		foreach (var action in actions)
		{
			if (!projection.TryEnqueue(action))
				throw new InvalidOperationException($"Cannot project illegal action {action}.");
			if (action.ActorId == actorId && action is MoveStepAction)
				checkpoints.Add(CaptureCheckpoint(projection.StateOf<State>(actorId)));
		}

		return checkpoints;
	}

	private static MoveCheckpoint CaptureCheckpoint(State state) =>
		new(
			state.Position,
			GridBasis.From(state.Fore, state.Dorsal, state.Starboard));

	private static bool StartsWith(IReadOnlyList<IAction> actions, IReadOnlyList<IAction> prefix)
	{
		if (actions.Count < prefix.Count)
			return false;

		for (var i = 0; i < prefix.Count; i++)
		{
			if (!Equals(actions[i], prefix[i]))
				return false;
		}

		return true;
	}

	internal static bool IsMovementAction(IAction action) =>
		action is MoveStepAction or HeadingTurnAction or RollAction;

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

			var comparison = left.ExtensionApCost.CompareTo(right.ExtensionApCost);
			if (comparison != 0)
				return comparison;

			comparison = left.Steps.Count(step => step is HeadingTurnAction)
				.CompareTo(right.Steps.Count(step => step is HeadingTurnAction));
			if (comparison != 0)
				return comparison;

			comparison = left.Steps.Count(step => step is RollAction)
				.CompareTo(right.Steps.Count(step => step is RollAction));
			if (comparison != 0)
				return comparison;

			for (var i = 0; i < left.Steps.Count; i++)
			{
				comparison = ActionOrder(left.Steps[i]).CompareTo(ActionOrder(right.Steps[i]));
				if (comparison != 0)
					return comparison;
			}

			return 0;
		}

		private static int ActionOrder(IAction action) =>
			action switch
			{
				MoveStepAction => 0,
				HeadingTurnAction { Turn: Movement.Enums.EHeadingTurn.YawLeft } => 1,
				HeadingTurnAction { Turn: Movement.Enums.EHeadingTurn.YawRight } => 2,
				HeadingTurnAction { Turn: Movement.Enums.EHeadingTurn.PitchUp } => 3,
				HeadingTurnAction { Turn: Movement.Enums.EHeadingTurn.PitchDown } => 4,
				RollAction { Direction: Movement.Enums.ERollDirection.Clockwise } => 5,
				RollAction { Direction: Movement.Enums.ERollDirection.CounterClockwise } => 6,
				_ => int.MaxValue,
			};
	}
}
