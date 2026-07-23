using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

/// <summary>
/// Stateful planning workspace: anchor world, preview fork, per-actor runtimes, and action queue.
/// </summary>
public class Simulation<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private readonly List<IAction> _actions = [];
	private readonly TWorld _anchorWorld;
	private readonly ActorRuntimes<TRuntime> _anchorActorRuntimes;
	private int _anchorTick;
	private int _nextUndoGroup;

	public Simulation(TWorld anchorWorld, ActorRuntimes<TRuntime> anchorActorRuntimes)
	{
		_anchorWorld = anchorWorld;
		_anchorActorRuntimes = anchorActorRuntimes;
	}

	public TWorld PreviewWorld { get; private set; } = default!;

	public ActorRuntimes<TRuntime> PreviewActorRuntimes { get; private set; } = default!;

	public IReadOnlyList<IAction> Actions => _actions;

	public int AnchorTick => _anchorTick;

	public TWorld AnchorWorld => _anchorWorld;

	public ActorRuntimes<TRuntime> AnchorActorRuntimes => _anchorActorRuntimes;

	public int WorldVersion { get; private set; }

	public bool IsStale(int currentWorldVersion) => WorldVersion != currentWorldVersion;

	public void Begin(int anchorTick, int worldVersion)
	{
		_anchorTick = anchorTick;
		WorldVersion = worldVersion;
		_actions.Clear();
		_nextUndoGroup = 0;
		Reevaluate();
	}

	public int AllocateUndoGroup() => ++_nextUndoGroup;

	public bool TryEnqueue(IAction action)
	{
		Reevaluate();
		if (action is not IAction<TWorld, TRuntime> typed)
			return false;

		var runtime = PreviewActorRuntimes.For(action);
		if (!typed.Definition.IsLegal(action, PreviewWorld, runtime))
			return false;

		_actions.Add(action);
		Reevaluate();
		return true;
	}

	public IEnumerable<TickResult> StepPreview(int ticksToAdvance) =>
		TimelineRunner.Step(PreviewWorld.Timeline, PreviewWorld, PreviewActorRuntimes, ticksToAdvance);

	public void AdvanceTo(int endTick)
	{
		var ticksToAdvance = endTick - PreviewWorld.Timeline.Clock.Current;
		if (ticksToAdvance <= 0)
			return;

		foreach (var _ in StepPreview(ticksToAdvance)) { }
	}

	public void Refresh() => Reevaluate();

	public bool TryUndoLast()
	{
		if (_actions.Count == 0)
			return false;

		PopUndoGroup();
		Reevaluate();
		return true;
	}

	public Plan Commit() => new(_actions.ToList());

	public void Reevaluate()
	{
		PreviewWorld = _anchorWorld.Fork();
		PreviewActorRuntimes = _anchorActorRuntimes.Fork();

		foreach (var action in _actions)
			ExecutionHelper.Apply(action, PreviewWorld, PreviewActorRuntimes.For(action));
	}

	private void PopUndoGroup()
	{
		var last = _actions[^1];
		if (last.UndoGroup is not int group)
		{
			_actions.RemoveAt(_actions.Count - 1);
			return;
		}

		while (_actions.Count > 0 && _actions[^1].UndoGroup == group)
			_actions.RemoveAt(_actions.Count - 1);
	}
}
