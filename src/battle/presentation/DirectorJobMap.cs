using System.Diagnostics;

namespace GrimSpace.Battle.Presentation;

internal static class DirectorJobs
{
	public const string Resolve = "resolve";
	public const string MovePrep = "movePrep";
}

/// <summary>
/// Versioned named async jobs for <see cref="BattleDirector"/>: start, cancel, await with stale detection.
/// </summary>
internal sealed class DirectorJobMap
{
	private readonly Dictionary<string, Slot> _slots = new();

	public void Cancel(string key)
	{
		var slot = GetOrCreate(key);
		slot.Version++;
	}

	public void Start(string key, Func<int, Task> work)
	{
		var slot = GetOrCreate(key);
		var version = ++slot.Version;
		slot.StartedVersion = version;
		slot.Task = work(version);
	}

	public async Task<(T? Value, bool IsCurrent, TimeSpan Elapsed)> Await<T>(string key)
	{
		var slot = GetOrCreate(key);
		if (slot.Task is not Task<T> task)
			return (default, false, TimeSpan.Zero);

		var startedVersion = slot.StartedVersion;
		var sw = Stopwatch.StartNew();
		try
		{
			var value = await task;
			sw.Stop();
			return (value, startedVersion == slot.Version, sw.Elapsed);
		}
		catch
		{
			sw.Stop();
			return (default, startedVersion == slot.Version, sw.Elapsed);
		}
	}

	public bool IsCurrent(string key, int version) =>
		GetOrCreate(key).Version == version;

	public void Clear(string key) => _slots.Remove(key);

	private Slot GetOrCreate(string key) =>
		_slots.TryGetValue(key, out var slot) ? slot : _slots[key] = new Slot();

	private sealed class Slot
	{
		public int Version;
		public int StartedVersion;
		public Task? Task;
	}
}
