using GrimSpace.Battle;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Targeting;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record MarkWeakspotAction(string ActorId, string TargetId, ESpatialOrientation Face)
	: IAction<BattleWorld, ActorRuntime>, IOrientedAction, ITargetedAction
{
	public ESpatialOrientation Orientation => Face;

	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		MarkWeakspotDef.Instance;
}

public sealed class MarkWeakspotDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IOrientedActionDef,
		ITargetedActionDef
{
	public static MarkWeakspotDef Instance { get; } = new();

	public static TargetPickSpec TargetPick { get; } = new(
		1,
		1,
		[
			new RelationFilter(EUnitRelation.Opponent),
			new MaxRangeFilter(PilotSkillPercents.MarkWeakspot.RangeCells),
		]);

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!runtime.PilotCapabilities.Contains(EPilotCapabilityId.MarkWeakspot))
			yield break;

		foreach (var targetId in TargetPick.LegalTargets(world, actorId, Array.Empty<string>()))
		{
			foreach (var face in Enum.GetValues<ESpatialOrientation>())
			{
				var action = Bind(actorId, targetId, face);
				if (IsLegal(action, world, runtime))
					yield return action;
			}
		}
	}

	public MarkWeakspotAction Bind(string actorId, string targetId, ESpatialOrientation face) =>
		new(actorId, targetId, face);

	IAction IOrientedActionDef.Bind(string actorId, ESpatialOrientation orientation) =>
		throw new InvalidOperationException("Mark Weakspot requires a target and face.");

	IAction ITargetedActionDef.Bind(string actorId, IReadOnlyList<string> pickedIds) =>
		throw new InvalidOperationException("Mark Weakspot requires a face after the target pick.");

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(MarkWeakspotAction action, BattleWorld world, ActorRuntime runtime) =>
		runtime.PilotCapabilities.Contains(EPilotCapabilityId.MarkWeakspot);

	public bool IsLegal(MarkWeakspotAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var actor = world.StateOf(action.ActorId);
		if (BattleStatuses.HasOverheat(actor) || actor.MarkWeakspotCooldownRemaining > 0)
			return false;
		if (!TargetPickSpec.IsLiving(world, action.TargetId))
			return false;
		return TargetPick.Filters.All(filter =>
			filter.Matches(world, action.ActorId, action.TargetId, Array.Empty<string>()));
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		MarkWeakspotAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actorId = action.ActorId;
		var targetId = action.TargetId;
		return
		[
			new HeatChangeEffect(HeatChangeEffect.PerAbilityUse),
			new SetMarkWeakspotDebuffEffect(targetId, action.Face, active: true),
			new SetMarkWeakspotCooldownEffect(PilotSkillPercents.MarkWeakspot.CooldownTurns),
			new ScheduleActivationRoundEffect(
				EUnitActivationPhase.End,
				PilotSkillPercents.MarkWeakspot.DurationEndPasses,
				sticky: false,
				new ClearMarkWeakspotAction(targetId),
				onUnitId: actorId),
		];
	}

	private static MarkWeakspotAction Cast(IAction action) =>
		action is MarkWeakspotAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(MarkWeakspotAction)}.", nameof(action));
}
