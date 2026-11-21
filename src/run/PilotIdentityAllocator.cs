using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Run;

public sealed class PilotIdentityAllocator
{
	private readonly HashSet<string> _usedNames = new(StringComparer.Ordinal);
	private readonly HashSet<string> _usedNicknames = new(StringComparer.Ordinal);

	public IReadOnlySet<string> UsedNames => _usedNames;
	public IReadOnlySet<string> UsedNicknames => _usedNicknames;

	public (string Name, string Nickname) Roll(
		EPilotBackgroundId backgroundId,
		IReadOnlyList<EPilotQuirkId> quirkIds,
		Random rng)
	{
		ArgumentNullException.ThrowIfNull(quirkIds);
		ArgumentNullException.ThrowIfNull(rng);
		var name = RobotNameCatalog.Roll(rng, _usedNames);
		var nickname = PilotNicknameCatalog.Roll(backgroundId, quirkIds, rng, _usedNicknames);
		Claim(name, nickname);
		return (name, nickname);
	}

	public (string Name, string Nickname) Suggest(
		EPilotBackgroundId backgroundId,
		IReadOnlyList<EPilotQuirkId> quirkIds,
		Random rng,
		ISet<string>? batchNames = null,
		ISet<string>? batchNicknames = null)
	{
		ArgumentNullException.ThrowIfNull(quirkIds);
		ArgumentNullException.ThrowIfNull(rng);
		var names = Combined(_usedNames, batchNames);
		var nicknames = Combined(_usedNicknames, batchNicknames);
		var name = RobotNameCatalog.Roll(rng, names);
		var nickname = PilotNicknameCatalog.Roll(backgroundId, quirkIds, rng, nicknames);
		batchNames?.Add(name);
		batchNicknames?.Add(nickname);
		return (name, nickname);
	}

	public void Claim(string name, string nickname)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(nickname);
		_usedNames.Add(name);
		_usedNicknames.Add(nickname);
	}

	public void Release(string name, string nickname)
	{
		if (!string.IsNullOrEmpty(name))
			_usedNames.Remove(name);
		if (!string.IsNullOrEmpty(nickname))
			_usedNicknames.Remove(nickname);
	}

	public void Reassign(
		string oldName,
		string oldNickname,
		string newName,
		string newNickname,
		IEnumerable<Pilot> otherPilots)
	{
		ArgumentNullException.ThrowIfNull(otherPilots);
		ArgumentException.ThrowIfNullOrEmpty(newName);
		ArgumentException.ThrowIfNullOrEmpty(newNickname);

		var others = otherPilots as IList<Pilot> ?? otherPilots.ToArray();
		if (!others.Any(pilot => string.Equals(pilot.Name, oldName, StringComparison.Ordinal)))
			_usedNames.Remove(oldName);
		if (!others.Any(pilot => string.Equals(pilot.Nickname, oldNickname, StringComparison.Ordinal)))
			_usedNicknames.Remove(oldNickname);

		Claim(newName, newNickname);
	}

	public void RebuildFrom(IEnumerable<Pilot> pilots)
	{
		ArgumentNullException.ThrowIfNull(pilots);
		_usedNames.Clear();
		_usedNicknames.Clear();
		foreach (var pilot in pilots)
			Claim(pilot.Name, pilot.Nickname);
	}

	public PilotIdentityAllocatorDto CaptureSnapshot() =>
		new(_usedNames.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
			_usedNicknames.OrderBy(name => name, StringComparer.Ordinal).ToArray());

	public void RestoreSnapshot(PilotIdentityAllocatorDto? dto)
	{
		_usedNames.Clear();
		_usedNicknames.Clear();
		if (dto is null)
			return;
		foreach (var name in dto.UsedNames)
			_usedNames.Add(name);
		foreach (var nickname in dto.UsedNicknames)
			_usedNicknames.Add(nickname);
	}

	private static HashSet<string> Combined(HashSet<string> primary, ISet<string>? extra)
	{
		if (extra is null || extra.Count == 0)
			return primary;
		var merged = new HashSet<string>(primary, StringComparer.Ordinal);
		merged.UnionWith(extra);
		return merged;
	}
}
