using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle;

public sealed record TurnReplay(
	IReadOnlyDictionary<string, State> StartStates,
	IReadOnlyList<ITimelineEntry> History,
	IReadOnlyDictionary<string, State> EndStates)
{
	public IEnumerable<IAction> Actions =>
		History.OfType<IAction>();

	public IReadOnlyDictionary<string, IReadOnlyList<IAction>> ActionsByActor =>
		History
			.OfType<IAction>()
			.GroupBy(action => action.ActorId, StringComparer.Ordinal)
			.ToDictionary(
				group => group.Key,
				group => (IReadOnlyList<IAction>)group.ToList(),
				StringComparer.Ordinal);
}
