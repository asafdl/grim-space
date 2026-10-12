using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed class ShieldsUpActivateEffect : IEffect<BattleWorld, ActorRuntime>
{
	private FaceShieldPoints? _previousPendingRestore;
	private int _previousPendingApPenalty;
	private bool _previousUsedThisTurn;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousPendingRestore = actor.PendingShieldRestore.Clone();
		_previousPendingApPenalty = actor.PendingApPenalty;
		_previousUsedThisTurn = actor.ShieldsUpUsedThisTurn;

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			actor.PendingShieldRestore[face] += ShieldsUpDef.ShieldRestorePerFace;

		actor.PendingApPenalty += ShieldsUpDef.NextUpkeepApPenalty;
		actor.ShieldsUpUsedThisTurn = true;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_previousPendingRestore is null)
			return;

		var actor = world.StateOf(actorId);
		actor.PendingShieldRestore = _previousPendingRestore;
		actor.PendingApPenalty = _previousPendingApPenalty;
		actor.ShieldsUpUsedThisTurn = _previousUsedThisTurn;
	}
}
