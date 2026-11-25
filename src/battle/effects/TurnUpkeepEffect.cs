using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class TurnUpkeepEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousActionPoints;
	private int _previousManeuverPoints;
	private int _previousHeat;
	private int _previousReactiveShieldsStacks;
	private string[] _previousCooledPlumes = [];
	private Dictionary<AbilityMount, MountRuntimeCounters>? _previousMountRuntime;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousActionPoints = actor.CurrentActionPoints;
		_previousManeuverPoints = actor.CurrentManeuverPoints;
		_previousHeat = actor.Heat;
		_previousReactiveShieldsStacks = actor.StatusOf<ReactiveShields>()?.Stacks ?? 0;
		_previousCooledPlumes = [.. actor.CryoPlumesCooledThisTurn];
		_previousMountRuntime = actor.MountRuntime.ToDictionary(
			entry => entry.Key,
			entry => entry.Value.Clone());

		actor.CryoPlumesCooledThisTurn.Clear();
		actor.CurrentActionPoints = actor.Maneuverability.BaseActionPoints;
		actor.CurrentManeuverPoints = actor.Maneuverability.BaseManeuverPoints;
		var records = new List<IRecord>();
		if (actor.StatusOf<ReactiveShields>() is { Stacks: > 0 } reactive)
		{
			reactive.Stacks = 0;
			records.Add(new Record<ReactiveShieldsChangedFacts>(new(actorId, reactive.Stacks)));
		}
		foreach (var installed in actor.Loadout.InstalledAbilities)
			installed.Spec.AdvanceRound(actor.MountRuntime[installed.Mount]);

		if (actor.OverchargeEnginesCooldownRemaining > 0)
			actor.OverchargeEnginesCooldownRemaining--;

		if (actor.MarkWeakspotCooldownRemaining > 0)
			actor.MarkWeakspotCooldownRemaining--;

		if (actor.HeatProfile.Participates
			&& !actor.Overdrive
			&& !actor.Overheat)
		{
			var clearPerTurn = actor.HeatProfile.ClearPerTurn
				+ (world.CryoPlumeAt(actor.Position) is null ? 0 : CryoPlume.HeatCleared);
			var cleared = System.Math.Max(0, actor.Heat - clearPerTurn);
			if (cleared != actor.Heat)
			{
				actor.Heat = cleared;
				records.Add(new Record<HeatChangedFacts>(new HeatChangedFacts(
					actorId,
					actor.Heat,
					actor.Overdrive,
					actor.Overheat)));
			}
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.CurrentActionPoints = _previousActionPoints;
		actor.CurrentManeuverPoints = _previousManeuverPoints;
		actor.Heat = _previousHeat;
		if (actor.StatusOf<ReactiveShields>() is { } reactive)
			reactive.Stacks = _previousReactiveShieldsStacks;
		actor.CryoPlumesCooledThisTurn.Clear();
		actor.CryoPlumesCooledThisTurn.UnionWith(_previousCooledPlumes);
		if (_previousMountRuntime is null)
			return;

		actor.MountRuntime.Clear();
		foreach (var (mount, snapshot) in _previousMountRuntime)
			actor.MountRuntime[mount] = snapshot;
	}
}
