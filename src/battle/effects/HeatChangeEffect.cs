using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class HeatChangeEffect(int delta) : IEffect<BattleWorld, ActorRuntime>
{
	public const int PerApSpent = 10;
	public const int PerMpSpent = 5;
	public const int PerAbilityUse = 20;
	public const int OverheatDelayTicks = 1;

	private int _previousHeat;
	private bool _previousOverdrive;
	private bool _applied;
	private int _scheduledTick;
	private EnterOverheatAction? _scheduledOverheat;

	public int Delta => delta;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousHeat = actor.Heat;
		_previousOverdrive = actor.Overdrive;
		_applied = false;
		_scheduledOverheat = null;

		if (!actor.HeatProfile.Participates || actor.Overheat || delta == 0)
			return [];

		var appliedDelta = delta > 0
			? actor.HeatProfile.ScaleGain(delta)
			: delta;
		if (appliedDelta == 0)
			return [];

		var capacity = actor.HeatProfile.EffectiveCapacity;
		var next = actor.Heat + appliedDelta;
		if (next < 0)
			next = 0;
		else if (next > capacity)
			next = capacity;

		actor.Heat = next;
		if (appliedDelta > 0 && _previousHeat < capacity && next >= capacity)
		{
			actor.Overdrive = true;
			_scheduledOverheat = new EnterOverheatAction(actorId);
			_scheduledTick = world.Timeline.Clock.Current + OverheatDelayTicks;
			world.Timeline.Schedule(OverheatDelayTicks, _scheduledOverheat);
		}

		_applied = true;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			return;

		if (_scheduledOverheat is not null)
		{
			world.Timeline.CancelPending(_scheduledTick, _scheduledOverheat);
			_scheduledOverheat = null;
		}

		var actor = world.StateOf(actorId);
		actor.Heat = _previousHeat;
		actor.Overdrive = _previousOverdrive;
	}
}
