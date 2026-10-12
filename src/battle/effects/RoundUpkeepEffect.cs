using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed class RoundUpkeepEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousActionPoints;
	private int _previousManeuverPoints;
	private Dictionary<AbilityMount, MountRuntimeCounters>? _previousMountRuntime;
	private bool _previousApPenaltyNextTurn;
	private int _previousPendingApPenalty;
	private FaceShieldPoints? _previousShieldPoints;
	private FaceShieldPoints? _previousPendingShieldRestore;
	private bool _previousShieldsUpUsedThisTurn;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousActionPoints = actor.ActionPoints;
		_previousManeuverPoints = actor.ManeuverPoints;
		_previousApPenaltyNextTurn = actor.ApPenaltyNextTurn;
		_previousPendingApPenalty = actor.PendingApPenalty;
		_previousShieldPoints = actor.ShieldPoints.Clone();
		_previousPendingShieldRestore = actor.PendingShieldRestore.Clone();
		_previousShieldsUpUsedThisTurn = actor.ShieldsUpUsedThisTurn;
		_previousMountRuntime = actor.MountRuntime.ToDictionary(
			entry => entry.Key,
			entry => entry.Value.Clone());

		PilotSkillUpkeep.ApplyPendingShieldRestore(actor);

		var apPenalty = actor.PendingApPenalty + (actor.ApPenaltyNextTurn ? 1 : 0);
		actor.PendingApPenalty = 0;
		actor.ApPenaltyNextTurn = false;
		actor.ShieldsUpUsedThisTurn = false;

		var maxAp = actor.Maneuverability.MaxActionPoints;
		if (apPenalty > 0)
			maxAp = System.Math.Max(0, maxAp - apPenalty);

		actor.ActionPoints = maxAp;
		actor.ManeuverPoints = actor.Maneuverability.MaxManeuverPoints;
		foreach (var installed in actor.Loadout.InstalledAbilities)
			installed.Spec.AdvanceRound(actor.MountRuntime[installed.Mount]);

		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ActionPoints = _previousActionPoints;
		actor.ManeuverPoints = _previousManeuverPoints;
		actor.ApPenaltyNextTurn = _previousApPenaltyNextTurn;
		actor.PendingApPenalty = _previousPendingApPenalty;
		if (_previousShieldPoints is not null)
			actor.ShieldPoints = _previousShieldPoints;
		if (_previousPendingShieldRestore is not null)
			actor.PendingShieldRestore = _previousPendingShieldRestore;
		actor.ShieldsUpUsedThisTurn = _previousShieldsUpUsedThisTurn;
		if (_previousMountRuntime is null)
			return;

		actor.MountRuntime.Clear();
		foreach (var (mount, snapshot) in _previousMountRuntime)
			actor.MountRuntime[mount] = snapshot;
	}
}
