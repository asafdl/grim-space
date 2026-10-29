using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class TurnUpkeepEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousActionPoints;
	private int _previousManeuverPoints;
	private int _previousHeat;
	private Dictionary<AbilityMount, MountRuntimeCounters>? _previousMountRuntime;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousActionPoints = actor.ActionPoints;
		_previousManeuverPoints = actor.ManeuverPoints;
		_previousHeat = actor.Heat;
		_previousMountRuntime = actor.MountRuntime.ToDictionary(
			entry => entry.Key,
			entry => entry.Value.Clone());

		actor.ActionPoints = actor.Maneuverability.MaxActionPoints;
		actor.ManeuverPoints = actor.Maneuverability.MaxManeuverPoints;
		foreach (var installed in actor.Loadout.InstalledAbilities)
			installed.Spec.AdvanceRound(actor.MountRuntime[installed.Mount]);

		if (actor.OverchargeEnginesCooldownRemaining > 0)
			actor.OverchargeEnginesCooldownRemaining--;

		if (actor.HeatProfile.Participates && !actor.Overdrive && !actor.Overheat)
		{
			var cleared = System.Math.Max(0, actor.Heat - actor.HeatProfile.ClearPerTurn);
			if (cleared != actor.Heat)
			{
				actor.Heat = cleared;
				return
				[
					new Record<HeatChangedFacts>(new HeatChangedFacts(
						actorId,
						actor.Heat,
						actor.Overdrive,
						actor.Overheat)),
				];
			}
		}

		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ActionPoints = _previousActionPoints;
		actor.ManeuverPoints = _previousManeuverPoints;
		actor.Heat = _previousHeat;
		if (_previousMountRuntime is null)
			return;

		actor.MountRuntime.Clear();
		foreach (var (mount, snapshot) in _previousMountRuntime)
			actor.MountRuntime[mount] = snapshot;
	}
}
