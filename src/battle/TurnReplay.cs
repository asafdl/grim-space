using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle;

/// <summary>
/// Turn boundary package for presentation: start snapshot, applied actions, end snapshot.
/// </summary>
public sealed record TurnReplay(
	IReadOnlyDictionary<string, State> StartStates,
	IReadOnlyList<IAction> AppliedActions,
	IReadOnlyDictionary<string, State> EndStates);
