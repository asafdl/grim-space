using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay;

internal static class VoidBombDetonationShake
{
	public static int CountDeaths(
		string sourceId,
		IReadOnlyList<IRecord> followingRecords,
		IReadOnlyDictionary<string, State> endStates)
	{
		var deaths = 0;
		foreach (var record in followingRecords.OfType<Record<ImpactFacts>>())
		{
			var impact = record.Value;
			if (impact.Cause != EImpactCause.VoidBombBlast || impact.SourceId != sourceId)
				continue;

			if (endStates.TryGetValue(impact.TargetId, out var end) && !end.IsAlive)
				deaths++;
		}

		return deaths;
	}
}
