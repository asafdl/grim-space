using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Dfs;

internal enum BridgeDecision { Continue, PruneChildren, Stop }

// Path holds the actions after the source simulation's queued prefix.
internal readonly record struct BridgeFrame(IAction[] Path, SearchFramePhase Phase);

// UpperBound covers this state and every descendant; int.MinValue denotes an ineligible score.
internal readonly record struct SearchEvaluation(int Score, int UpperBound);

// Entry point to the Go action search (src/core/dfs/native). Go owns traversal, visit dominance, and
// goroutine scheduling over paths of opaque action handles; C# owns every rule through SearchAdapter.
internal static class SearchBridge
{
	public const int MaxSearchDepth = 20;
	public const int CallbackCapacity = 4096;
	public const int MaxPathLength = 64;

	public static SearchBridge<TEffect, TWorld, TRuntime> Run<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> source,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> definitions,
		SearchInput<TWorld, TRuntime> input,
		Func<SearchFrame<TWorld, TRuntime>, BridgeDecision>? decide = null,
		int workers = 0)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new() =>
		new(source, actorId, definitions, input, decide, workers);

	public static SearchBridge<TEffect, TWorld, TRuntime> FindBest<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> source,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> definitions,
		SearchInput<TWorld, TRuntime> input,
		Func<Simulation<TWorld, TRuntime>, SearchEvaluation> evaluate,
		int workers,
		CancellationToken cancellationToken = default)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new() =>
		new(source, actorId, definitions, input, null, workers, evaluate, cancellationToken);

	public static IEnumerable<SearchFrame<TWorld, TRuntime>> Enumerate<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> source,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> definitions,
		SearchInput<TWorld, TRuntime> input,
		CancellationToken cancellationToken = default)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (definitions.Count == 0)
			throw new NotSupportedException("The bridge requires at least one action definition.");
		var tables = new SearchTables();
		var adapter = new SearchAdapter<TEffect, TWorld, TRuntime>(
			source, actorId, definitions, input, null, tables, cancellationToken: cancellationToken);
		Rules rules = (worker, operation, argument, path, depth, output, capacity) =>
			worker == 0 ? adapter.Evaluate(operation, argument, path, depth, output, capacity) : -1;
		var priority = input.IsPriorityBranch?.Invoke(source.Actions) ?? false;
		if (Native.Start(definitions.Count, priority ? 1 : 0, CallbackCapacity, out var id) < 0)
			throw new InvalidOperationException("Unable to start incremental Go search.");
		using var cursor = new CursorHandle(id);
		var buffer = new int[MaxPathLength + 2];
		SearchFrame<TWorld, TRuntime>? frame = null;
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var result = Native.Next(cursor, rules,
				frame?.PruneChildren == true ? (int)BridgeDecision.PruneChildren : (int)BridgeDecision.Continue,
				buffer, buffer.Length, out var used);
			adapter.Failure?.Throw();
			if (result < 0)
				throw new InvalidOperationException("Incremental Go search failed.");
			if (result == 0)
				yield break;
			var phase = (SearchFramePhase)buffer[0];
			var depth = buffer[1];
			if (depth < 0 || depth > MaxPathLength || used != depth + 2
				|| phase is not (SearchFramePhase.Priority or SearchFramePhase.Remaining))
				throw new InvalidOperationException("Go search returned a malformed incremental frame.");
			frame = adapter.Capture(depth, phase);
			yield return frame;
		}
	}

	public static (int Parallelism, bool RaceEnabled) RuntimeInfo()
	{
		if (Native.RuntimeInfo(out var parallelism, out var race) != 1)
			throw new InvalidOperationException("Unable to read Go runtime configuration.");
		return (parallelism, race != 0);
	}

	// Moves the simulation from its current queue to prefix + path, reusing the longest shared prefix.
	public static void MoveTo<TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> simulation, int prefix, IReadOnlyList<IAction> path)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		var queued = simulation.Actions;
		var shared = 0;
		while (shared < path.Count && prefix + shared < queued.Count
			&& ReferenceEquals(queued[prefix + shared], path[shared]))
			shared++;
		if (queued.Count > prefix + shared)
			simulation.Dequeue(prefix + shared);
		for (var step = shared; step < path.Count; step++)
			if (!simulation.TryEnqueue(path[step]))
				throw new InvalidOperationException($"Search path replay rejected {path[step]}.");
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int Rules(int worker, int operation, int argument, nint path, int depth, nint output, int capacity);

	internal sealed class CursorHandle : SafeHandle
	{
		internal CursorHandle(ulong value) : base(nint.Zero, ownsHandle: true) =>
			SetHandle(checked((nint)value));

		public override bool IsInvalid => handle == nint.Zero;

		protected override bool ReleaseHandle() => Native.Destroy((ulong)handle) == 1;
	}

	internal static class Native
	{
		private const string Library = "grim_space_search";

		[DllImport(Library, EntryPoint = "bridge_search", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Search(
			int familyCount, int priority, int capacity, int workers, Rules rules, out ulong handle);

		[DllImport(Library, EntryPoint = "bridge_search_best", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int FindBest(
			int familyCount, int capacity, int workers, Rules rules,
			out ulong handle, [Out] long[] statistics);

		[DllImport(Library, EntryPoint = "bridge_search_start", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Start(int familyCount, int priority, int capacity, out ulong handle);

		[DllImport(Library, EntryPoint = "bridge_search_next", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Next(
			CursorHandle handle, Rules rules, int decision, [Out] int[] buffer, int capacity, out int used);

		[DllImport(Library, EntryPoint = "bridge_search_count", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Count(ulong handle);

		[DllImport(Library, EntryPoint = "bridge_search_page", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Page(
			ulong handle, int start, int limit, [Out] int[] buffer, int capacity, out int used);

		[DllImport(Library, EntryPoint = "bridge_allocated", CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong Allocated();

		[DllImport(Library, EntryPoint = "bridge_collect", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void Collect();

		[DllImport(Library, EntryPoint = "bridge_runtime_info", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int RuntimeInfo(out int parallelism, out int race);

		[DllImport(Library, EntryPoint = "bridge_destroy", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Destroy(ulong handle);
	}
}

// Run retains frames; FindBest retains only the winning path. Each worker owns a simulation cursor.
internal sealed class SearchBridge<TEffect, TWorld, TRuntime>
	where TEffect : IEffect<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private readonly SearchAdapter<TEffect, TWorld, TRuntime>[] _adapters;
	private readonly SearchTables _tables = new();
	private readonly SearchBridge.Rules _rules;

	public SearchBridge(
		Simulation<TWorld, TRuntime> source,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> definitions,
		SearchInput<TWorld, TRuntime> input,
		Func<SearchFrame<TWorld, TRuntime>, BridgeDecision>? decide,
		int workers,
		Func<Simulation<TWorld, TRuntime>, SearchEvaluation>? evaluate = null,
		CancellationToken cancellationToken = default)
	{
		if (workers is < 0 or > 64)
			throw new ArgumentOutOfRangeException(nameof(workers), "Use 0 for serial or 1-64 goroutine workers.");
		if (workers != 0 && decide is not null)
			throw new NotSupportedException("Consumer pruning/stopping is currently supported in serial mode only.");
		if (definitions.Count == 0)
			throw new NotSupportedException("The bridge requires at least one action definition.");
		if (evaluate is not null && (workers == 0 || input.IsPriorityBranch is not null))
			throw new NotSupportedException("Bounded search requires goroutine workers and does not use priority phases.");
		_adapters = new SearchAdapter<TEffect, TWorld, TRuntime>[workers + 1];
		for (var worker = 0; worker < _adapters.Length; worker++)
			_adapters[worker] = new(source, actorId, definitions, input, decide, _tables, evaluate, cancellationToken);
		_rules = Evaluate;
		var startPriority = input.IsPriorityBranch?.Invoke(source.Actions) ?? false;
		ulong handle;
		if (evaluate is null)
			Check(SearchBridge.Native.Search(definitions.Count, startPriority ? 1 : 0, SearchBridge.CallbackCapacity,
				workers, _rules, out handle));
		else
		{
			var statistics = new long[4];
			Check(SearchBridge.Native.FindBest(
				definitions.Count, SearchBridge.CallbackCapacity, workers, _rules, out handle, statistics));
			EvaluatedFrames = statistics[0];
			DominancePrunedFrames = statistics[1];
			BoundPrunedFrames = statistics[2];
			BestScore = checked((int)statistics[3]);
		}
		try
		{
			Frames = ReadFrames(handle);
		}
		finally
		{
			SearchBridge.Native.Destroy(handle);
		}
	}

	public IReadOnlyList<BridgeFrame> Frames { get; }
	public int Count => Frames.Count;
	public int WorkersUsed => _adapters.Skip(1).Count(adapter => adapter.CallbackCount != 0);
	public long EvaluatedFrames { get; }
	public long DominancePrunedFrames { get; }
	public long BoundPrunedFrames { get; }
	public int BestScore { get; } = int.MinValue;

	public SearchFrame<TWorld, TRuntime> FrameAt(int index) => _adapters[0].Materialize(Frames[index]);

	private List<BridgeFrame> ReadFrames(ulong handle)
	{
		const int pageSize = 256;
		var total = Check(SearchBridge.Native.Count(handle));
		var buffer = new int[(SearchBridge.MaxPathLength + 2) * pageSize];
		var result = new List<BridgeFrame>(total);
		while (result.Count < total)
		{
			var count = Check(SearchBridge.Native.Page(handle, result.Count, pageSize, buffer, buffer.Length, out var used));
			if (count == 0)
				throw new InvalidOperationException("Go search returned an incomplete result page.");
			var offset = 0;
			for (var index = 0; index < count; index++)
			{
				var phase = (SearchFramePhase)buffer[offset++];
				var path = new IAction[buffer[offset++]];
				for (var step = 0; step < path.Length; step++)
					path[step] = _tables[buffer[offset++]];
				result.Add(new BridgeFrame(path, phase));
			}
			if (offset != used)
				throw new InvalidOperationException("Go search returned a malformed result page.");
		}
		return result;
	}

	private int Check(int result)
	{
		foreach (var adapter in _adapters)
			adapter.Failure?.Throw();
		if (result < 0)
			throw new InvalidOperationException("Go search bridge operation failed.");
		return result;
	}

	private int Evaluate(int worker, int operation, int argument, nint path, int depth, nint output, int capacity)
	{
		if (worker < 0 || worker >= _adapters.Length)
			return -1;
		return _adapters[worker].Evaluate(operation, argument, path, depth, output, capacity);
	}
}

// Shared by all workers without locks. Actions are interned by value (they are records), so the table
// stays small and discovered duplicates die young. Handles index fixed chunks, so they never move, and
// Go only sees a handle after the callback that minted it returns.
internal sealed class SearchTables
{
	private const int ChunkBits = 16;
	private const int ChunkMask = (1 << ChunkBits) - 1;
	private readonly IAction[]?[] _chunks = new IAction[]?[1 << 14];
	private readonly ConcurrentDictionary<IAction, int> _actionHandles = new();
	private readonly ConcurrentDictionary<object, int> _visitKeys = new();
	private int _actionCount;
	private int _keyCount;

	public IAction this[int handle] => _chunks[handle >> ChunkBits]![handle & ChunkMask];

	public int Add(IAction action) =>
		_actionHandles.GetOrAdd(action, static (value, tables) => tables.Append(value), this);

	private int Append(IAction action)
	{
		var handle = Interlocked.Increment(ref _actionCount) - 1;
		var chunk = Volatile.Read(ref _chunks[handle >> ChunkBits]);
		if (chunk is null)
		{
			Interlocked.CompareExchange(ref _chunks[handle >> ChunkBits], new IAction[ChunkMask + 1], null);
			chunk = _chunks[handle >> ChunkBits]!;
		}
		chunk[handle & ChunkMask] = action;
		return handle;
	}

	public int KeyId(object key) =>
		_visitKeys.GetOrAdd(key, static (_, tables) => Interlocked.Increment(ref tables._keyCount), this);
}
