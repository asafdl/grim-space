using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record TorpedoAction(
	string ActorId,
	ETorpedoMount Mount,
	string? SpawnedUnitId = null) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		TorpedoDef.For(Mount);
}

public sealed class TorpedoDef(ETorpedoMount mount)
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public ETorpedoMount Mount { get; } = mount;

	public static TorpedoDef For(ETorpedoMount mount) => new(mount);

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public TorpedoAction Bind(string actorId) => new(actorId, Mount);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(TorpedoAction action, BattleWorld world, ActorRuntime runtime)
	{
		var ship = world.StateOf(action.ActorId);
		var (position, _, _) = TorpedoMount.LaunchPose(ship, action.Mount);
		return world.Grid.IsInBounds(position) && !world.BlockedFor(action.ActorId).Contains(position);
	}

	public bool IsLegal(TorpedoAction action, BattleWorld world, ActorRuntime runtime)
	{
		var firer = UnitRegistry.For(world).UnitOf(action.ActorId);
		if (firer.Controller != EController.Player)
			return false;
		if (firer.State.TorpedoCooldownRemaining > 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		TorpedoAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
	[
		new SpawnTorpedoEffect(action.Mount, action.SpawnedUnitId),
		new TorpedoCooldownEffect(TorpedoConfig.CooldownTurns),
	];

	private static TorpedoAction Cast(IAction action) =>
		action as TorpedoAction ?? throw new ArgumentException($"Expected {nameof(TorpedoAction)}.", nameof(action));
}
