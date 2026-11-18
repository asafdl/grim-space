using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Dfs;

// One per Go worker. Answers callbacks on a live Simulation that it moves to the requested path by
// dequeuing to the shared prefix and enqueuing the rest, so undo, timeline, runtimes, and action history
// stay exactly as the C# search would have them.
internal sealed class SearchAdapter<TEffect, TWorld, TRuntime>(
	Simulation<TWorld, TRuntime> source,
	string actorId,
	IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> definitions,
	SearchInput<TWorld, TRuntime> input,
	Func<SearchFrame<TWorld, TRuntime>, BridgeDecision>? decide,
	SearchTables tables,
	Func<Simulation<TWorld, TRuntime>, SearchEvaluation>? evaluate = null,
	CancellationToken cancellationToken = default)
	where TEffect : IEffect<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private readonly Simulation<TWorld, TRuntime> _simulation = source.Fork();
	private readonly int _prefix = source.Actions.Count;
	private readonly int[] _handles = new int[SearchBridge.MaxPathLength];
	// Handles queued on _simulation after the prefix; depth -1 means unknown (after Materialize).
	private readonly int[] _current = new int[SearchBridge.MaxPathLength];
	private int _currentDepth;
	private readonly int[] _output = new int[SearchBridge.CallbackCapacity];

	public long CallbackCount { get; private set; }
	public ExceptionDispatchInfo? Failure { get; private set; }

	public int Evaluate(int operation, int argument, nint path, int depth, nint output, int capacity)
	{
		CallbackCount++;
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (operation == 3 && decide is null)
				return (int)BridgeDecision.Continue;
			if (depth != 0)
				Marshal.Copy(path, _handles, 0, depth);
			Sync(depth);
			var count = 0;
			switch (operation)
			{
				case 0:
					var runtime = _simulation.RuntimeFor(actorId);
					foreach (var action in definitions[argument].Discover(_simulation.World, runtime, actorId).ToList())
					{
						if (input.ShouldExploreAction?.Invoke(_simulation, actorId, action) == false)
							continue;
						if (count == capacity)
							throw new NotSupportedException("Discovered actions exceed the native callback buffer.");
						_output[count++] = tables.Add(action);
					}
					break;
				case 1:
					if (!_simulation.TryEnqueue(tables[argument]))
						return 0;
					_current[_currentDepth++] = argument;
					return 1;
				case 2:
					var visit = input.VisitState(_simulation, actorId);
					if (visit.Budget.Length + 2 > capacity)
						throw new NotSupportedException("Visit budget exceeds the native callback buffer.");
					_output[count++] = _simulation.InvariantStatus == InvariantStatus.Ok ? 1 : 0;
					_output[count++] = tables.KeyId(visit.State);
					visit.Budget.CopyTo(_output, count);
					count += visit.Budget.Length;
					break;
				case 3:
					return (int)decide!(Capture(depth, (SearchFramePhase)argument));
				case 4:
					return input.IsPriorityBranch!(_simulation.Actions) ? 1 : 0;
				case 5:
					var evaluation = evaluate!(_simulation);
					if (evaluation.UpperBound < evaluation.Score)
						throw new InvalidOperationException("Search upper bound is lower than the current score.");
					_output[count++] = _simulation.InvariantStatus == InvariantStatus.Ok
						? evaluation.Score
						: int.MinValue;
					_output[count++] = evaluation.UpperBound;
					break;
				default:
					throw new NotSupportedException($"Unknown native search operation: {operation}.");
			}
			Marshal.Copy(_output, 0, output, count);
			return count;
		}
		catch (Exception exception)
		{
			Failure = ExceptionDispatchInfo.Capture(exception);
			return -1;
		}
	}

	public SearchFrame<TWorld, TRuntime> Materialize(BridgeFrame frame)
	{
		_currentDepth = -1;
		SearchBridge.MoveTo(_simulation, _prefix, frame.Path);
		return Capture(frame.Path.Length, frame.Phase);
	}

	// Moves the simulation to the requested handle path by comparing handles, not resolved actions.
	private void Sync(int depth)
	{
		if (_currentDepth < 0)
		{
			_simulation.Dequeue(_prefix);
			_currentDepth = 0;
		}
		var shared = 0;
		while (shared < depth && shared < _currentDepth && _handles[shared] == _current[shared])
			shared++;
		if (_currentDepth > shared)
			_simulation.Dequeue(_prefix + shared);
		for (_currentDepth = shared; _currentDepth < depth; _currentDepth++)
		{
			var action = tables[_current[_currentDepth] = _handles[_currentDepth]];
			if (!_simulation.TryEnqueue(action))
				throw new InvalidOperationException($"Search path replay rejected {action}.");
		}
	}

	public SearchFrame<TWorld, TRuntime> Capture(int depth, SearchFramePhase phase) =>
		new(_simulation.World.Fork(), _simulation.Runtimes.Fork(), _simulation.Actions.ToList(), depth, phase);
}
