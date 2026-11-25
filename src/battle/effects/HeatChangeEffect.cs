using GrimSpace.Battle.Actions;
using GrimSpace.Units;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class HeatChangeEffect : IEffect<BattleWorld, ActorRuntime>
{
	public const int PerApSpent = 10;
	public const int PerMpSpent = 5;
	public const int PerAbilityUse = 20;
	public const int OverheatDelayTicks = 1;
	public const int OverdriveDamageBonusPercent = ShipLoadout.BaselineOverdriveDamageBonusPercent;

	private int _previousHeat;
	private bool _previousOverdrive;
	private bool _applied;
	private int _scheduledTick;
	private EnterOverheatAction? _scheduledOverheat;
	private readonly int _delta;
	private readonly bool _forceToCapacity;

	public HeatChangeEffect(int delta) : this(delta, forceToCapacity: false) { }

	private HeatChangeEffect(int delta, bool forceToCapacity)
	{
		_delta = delta;
		_forceToCapacity = forceToCapacity;
	}

	public static HeatChangeEffect FillToCapacity() => new(0, forceToCapacity: true);

	public int Delta => _delta;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousHeat = actor.Heat;
		_previousOverdrive = actor.Overdrive;
		_applied = false;
		_scheduledOverheat = null;

		if (!actor.HeatProfile.Participates || actor.Overheat || (!_forceToCapacity && _delta == 0))
			return [];

		var appliedDelta = _delta > 0
			? actor.HeatProfile.ScaleGain(_delta)
			: _delta;
		if (!_forceToCapacity && appliedDelta == 0)
			return [];

		var capacity = actor.HeatProfile.EffectiveCapacity;
		var next = _forceToCapacity ? capacity : actor.Heat + appliedDelta;
		if (next < 0)
			next = 0;
		else if (next > capacity)
			next = capacity;

		actor.Heat = next;
		if (!_previousOverdrive
			&& (_forceToCapacity || appliedDelta > 0 && _previousHeat < capacity && next >= capacity))
		{
			actor.Overdrive = true;
			_scheduledOverheat = new EnterOverheatAction(actorId);
			_scheduledTick = world.Timeline.Clock.Current + OverheatDelayTicks;
			world.Timeline.Schedule(OverheatDelayTicks, _scheduledOverheat);
		}

		_applied = true;
		return
		[
			new Record<HeatChangedFacts>(new HeatChangedFacts(
				actorId,
				actor.Heat,
				actor.Overdrive,
				actor.Overheat)),
		];
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
