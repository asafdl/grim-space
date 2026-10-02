using GrimSpace.Battle.Units;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ids;
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

	public IReadOnlyList<string> ActivationOrder =>
		History
			.OfType<EndOfPhaseAction>()
			.Select(action => action.ActorId)
			.Where(actorId => actorId is not BattleActorIds.Rules and not BattleActorIds.Terrain)
			.ToList();
}
