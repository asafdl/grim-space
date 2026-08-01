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
		PresentationDiagnostics.LogJobCancelled(key, slot.Version);
	}

	public void Start(string key, Func<int, Task> work)
	{
		var slot = GetOrCreate(key);
		var version = ++slot.Version;
		slot.StartedVersion = version;
		slot.Task = work(version);
		PresentationDiagnostics.LogJobStarted(key, version);
	}

	public async Task<(T? Value, bool IsCurrent, TimeSpan Elapsed)> Await<T>(string key)
	{
		var slot = GetOrCreate(key);
		if (slot.Task is not Task<T> task)
		{
			PresentationDiagnostics.LogJobAwaited(key, slot.StartedVersion, slot.Version, hasTask: false, succeeded: false);
			return (default, false, TimeSpan.Zero);
		}

		var startedVersion = slot.StartedVersion;
		var sw = Stopwatch.StartNew();
		try
		{
			var value = await task;
			sw.Stop();
			var isCurrent = startedVersion == slot.Version;
			PresentationDiagnostics.LogJobAwaited(key, startedVersion, slot.Version, hasTask: true, succeeded: true);
			return (value, isCurrent, sw.Elapsed);
		}
		catch (Exception ex)
		{
			sw.Stop();
			PresentationDiagnostics.LogJobFailed(key, ex);
			PresentationDiagnostics.LogJobAwaited(key, startedVersion, slot.Version, hasTask: true, succeeded: false);
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
