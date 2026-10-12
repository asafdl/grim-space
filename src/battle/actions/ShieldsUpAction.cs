using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Actions;

public sealed record ShieldsUpAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ShieldsUpDef.Instance;
}

/// <summary>
/// Restores +1 shield per face on the following round upkeep (capped at loadout max).
/// Costs AP now and applies a stacked AP penalty on that upkeep.
/// </summary>
public sealed class ShieldsUpDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActorActionDef
{
	public const int ActionPointCost = 1;
	public const int NextUpkeepApPenalty = 2;
	public const int ShieldRestorePerFace = 1;

	public static ShieldsUpDef Instance { get; } = new();

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public ShieldsUpAction Bind(string actorId) => new(actorId);

	IAction IActorActionDef.Bind(string actorId) => Bind(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(ShieldsUpAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).PilotSkills.Contains(EPilotSkillId.ShieldsUp);

	public bool IsLegal(ShieldsUpAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var actor = world.StateOf(action.ActorId);
		return actor.ActionPoints >= ActionPointCost
			&& !actor.ShieldsUpUsedThisTurn;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		ShieldsUpAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
	[
		new ApChangeEffect(-ActionPointCost),
		new ShieldsUpActivateEffect(),
	];

	private static ShieldsUpAction Cast(IAction action) =>
		action is ShieldsUpAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(ShieldsUpAction)}.", nameof(action));
}
