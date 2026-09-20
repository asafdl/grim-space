using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class RoundUpkeepEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousActionPoints;
	private Dictionary<EAbilityKind, MountRuntimeCounters>? _previousMountRuntime;
	private bool _previousApPenaltyNextTurn;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousActionPoints = actor.ActionPoints;
		_previousApPenaltyNextTurn = actor.ApPenaltyNextTurn;
		_previousMountRuntime = actor.MountRuntime.ToDictionary(
			entry => entry.Key,
			entry => entry.Value.Clone());

		var maxAp = actor.Stats.MaxAp;
		if (actor.ApPenaltyNextTurn)
		{
			maxAp = System.Math.Max(0, maxAp - 1);
			actor.ApPenaltyNextTurn = false;
		}

		actor.ActionPoints = maxAp;
		foreach (var installed in actor.Spec.InstalledAbilities)
			installed.Spec.AdvanceRound(actor.MountRuntime[installed.Spec.Kind]);

		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ActionPoints = _previousActionPoints;
		actor.ApPenaltyNextTurn = _previousApPenaltyNextTurn;
		if (_previousMountRuntime is null)
			return;

		actor.MountRuntime.Clear();
		foreach (var (kind, snapshot) in _previousMountRuntime)
			actor.MountRuntime[kind] = snapshot;
	}
}
