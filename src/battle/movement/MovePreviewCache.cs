using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// Prefix-keyed move path cache for on-demand discovery from the live simulation.
/// </summary>
public sealed class MovePreviewCache
{
	private readonly Dictionary<string, IReadOnlyList<MovePathSession>> _pathsByPrefix = new();

	public IReadOnlyList<MovePathSession> GetPaths(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId,
		IReadOnlyList<IAction> committed)
	{
		var actions = sim.Actions;
		if (actions.Any(IsWeaponAction))
			return [];

		var key = PrefixKey(actions);
		if (_pathsByPrefix.TryGetValue(key, out var cached))
			return cached;

		var paths = MovePathEndpoints.DiscoverExtensions(sim, actorId);
		_pathsByPrefix[key] = paths;
		return paths;
	}

	public void Clear() => _pathsByPrefix.Clear();

	public static string PrefixKey(IReadOnlyList<IAction> actions) =>
		actions.Count == 0
			? string.Empty
			: string.Join('|', actions.Select(ActionKey));

	private static string ActionKey(IAction action) =>
		action switch
		{
			MoveStepAction move => $"move:{move.ActorId}:{move.Direction}",
			HeadingTurnAction heading => $"heading:{heading.ActorId}:{heading.Turn}",
			RollAction roll => $"roll:{roll.ActorId}:{roll.Direction}",
			FlakAction flak => $"flak:{flak.ActorId}:{flak.Mount}",
			RailgunAction railgun => $"railgun:{railgun.ActorId}",
			_ => action.GetType().FullName ?? "action",
		};

	private static bool IsWeaponAction(IAction action) =>
		action is FlakAction or RailgunAction;
}
