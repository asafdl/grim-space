using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// Prefix-keyed move path cache for the human player's planning turn (queue / undo).
/// </summary>
public sealed class MovePreviewCache
{
	private readonly Dictionary<string, IReadOnlyList<MovePathSession>> _pathsByPrefix = new();

	public IReadOnlyList<MovePathSession> GetPaths(
		Simulation<BattleWorld, ActorRuntime> sim,
		string playerId,
		IReadOnlyList<IAction> committed)
	{
		var key = PrefixKey(committed);
		if (_pathsByPrefix.TryGetValue(key, out var cached))
			return cached;

		var paths = MovePathEndpoints.DiscoverExtensions(sim, playerId);
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
			FlakAction flak => $"flak:{flak.ActorId}:{flak.MountedOn}",
			RailgunAction railgun => $"railgun:{railgun.ActorId}",
			TorpedoAction torpedo => $"torpedo:{torpedo.ActorId}:{torpedo.MountedOn}:{torpedo.SpawnedUnitId}",
			_ => action.GetType().FullName ?? "action",
		};
}
