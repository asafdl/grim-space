using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.ActivationRound;

public sealed class PerUnitActivationRoundLists
{
	private readonly Dictionary<string, List<ActivationRoundEntry>> _byUnit = new(StringComparer.Ordinal);

	public static PerUnitActivationRoundLists For(BattleWorld world) => world.ActivationRoundLists;

	public IReadOnlyList<ActivationRoundEntry> EntriesFor(string unitId) =>
		_byUnit.TryGetValue(unitId, out var list) ? list : [];

	internal void Append(string unitId, ActivationRoundEntry entry)
	{
		if (!_byUnit.TryGetValue(unitId, out var list))
		{
			list = [];
			_byUnit[unitId] = list;
		}

		list.Add(entry);
	}

	internal bool TryRemoveLastMatching(string unitId, ActivationRoundEntry entry)
	{
		if (!_byUnit.TryGetValue(unitId, out var list))
			return false;

		for (var i = list.Count - 1; i >= 0; i--)
		{
			if (list[i] == entry)
			{
				list.RemoveAt(i);
				return true;
			}
		}

		return false;
	}

	public IReadOnlyList<IAction> ProcessPass(string unitId, EUnitActivationPhase unitPhase)
	{
		if (!_byUnit.TryGetValue(unitId, out var list))
			return [];

		var published = new List<IAction>();
		for (var i = 0; i < list.Count;)
		{
			var entry = list[i];
			if (entry.RunAt != unitPhase)
			{
				i++;
				continue;
			}

			if (entry.RoundsRemaining is null)
			{
				published.Add(entry.Action);
				if (entry.Sticky)
					i++;
				else
					list.RemoveAt(i);
				continue;
			}

			var remaining = entry.RoundsRemaining.Value - 1;
			if (remaining > 0)
			{
				list[i] = entry with { RoundsRemaining = remaining };
				i++;
				continue;
			}

			published.Add(entry.Action);
			if (entry.Sticky)
				list[i] = entry with { RoundsRemaining = 0 };
			else
				list.RemoveAt(i);
		}

		return published;
	}

	public PerUnitActivationRoundLists CloneForFork()
	{
		var clone = new PerUnitActivationRoundLists();
		foreach (var (unitId, entries) in _byUnit)
			clone._byUnit[unitId] = entries.Select(entry => entry with { }).ToList();

		return clone;
	}
}
