using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Targeting;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Dev;

/// <summary>Dev-only 1–3 enemy paint used to exercise range target-pick UI.</summary>
public sealed record DevMarkTargetsAction(string ActorId, IReadOnlyList<string> TargetIds)
	: IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		DevMarkTargetsDef.Instance;
}

public sealed class DevMarkTargetsDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		ITargetedActionDef
{
	public static DevMarkTargetsDef Instance { get; } = new();

	public static TargetPickSpec TargetPick { get; } = new(
		1,
		3,
		[new RelationFilter(EUnitRelation.Opponent), new ExcludeAlreadyPickedFilter()]);

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!TargetPickShowcase.Enabled)
			yield break;

		foreach (var targetId in TargetPick.LegalTargets(world, actorId, Array.Empty<string>()))
		{
			var action = Bind(actorId, [targetId]);
			if (IsLegal(action, world, runtime))
				yield return action;
		}
	}

	public DevMarkTargetsAction Bind(string actorId, IReadOnlyList<string> targetIds) =>
		new(actorId, targetIds.ToArray());

	IAction ITargetedActionDef.Bind(string actorId, IReadOnlyList<string> pickedIds)
	{
		if (pickedIds.Count < TargetPick.MinPicks || pickedIds.Count > TargetPick.MaxPicks)
			throw new ArgumentException(
				$"Mark Targets requires {TargetPick.MinPicks}-{TargetPick.MaxPicks} enemies.",
				nameof(pickedIds));
		return Bind(actorId, pickedIds);
	}

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(DevMarkTargetsAction action, BattleWorld world, ActorRuntime runtime) =>
		TargetPickShowcase.Enabled;

	public bool IsLegal(DevMarkTargetsAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;
		if (BattleStatuses.HasOverheat(world.StateOf(action.ActorId)))
			return false;
		if (!TargetPick.CanLockIn(action.TargetIds.Count))
			return false;

		var picked = new List<string>();
		foreach (var targetId in action.TargetIds)
		{
			if (!TargetPickSpec.IsLiving(world, targetId))
				return false;
			if (!TargetPick.Filters.All(filter =>
				filter.Matches(world, action.ActorId, targetId, picked)))
				return false;
			picked.Add(targetId);
		}

		return true;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		DevMarkTargetsAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new HeatChangeEffect(HeatChangeEffect.PerAbilityUse)];

	private static DevMarkTargetsAction Cast(IAction action) =>
		action is DevMarkTargetsAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(DevMarkTargetsAction)}.", nameof(action));
}
