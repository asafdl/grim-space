using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle;

public sealed record TurnReplay(
	IReadOnlyDictionary<string, State> StartStates,
	IReadOnlyList<TimelineBatch> Batches,
	IReadOnlyDictionary<string, State> EndStates)
{
	public IEnumerable<IAction> Actions =>
		Batches.SelectMany(batch => batch.Actions);

	public IReadOnlyDictionary<string, IReadOnlyList<IAction>> ActionsByActor =>
		Batches
			.GroupBy(batch => batch.ActorId, StringComparer.Ordinal)
			.ToDictionary(
				group => group.Key,
				group => (IReadOnlyList<IAction>)group.SelectMany(batch => batch.Actions).ToList(),
				StringComparer.Ordinal);
}
