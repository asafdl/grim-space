using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record TorpedoAction(
	string ActorId,
	ESpatialOrientation MountedOn,
	string? SpawnedUnitId = null) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		TorpedoDef.Instance;
}

public sealed class TorpedoDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef
{
	public static TorpedoDef Instance { get; } = new();

	private static readonly ESpatialOrientation[] MountedOn =
	[
		ESpatialOrientation.Retro,
		ESpatialOrientation.Ventral,
		ESpatialOrientation.Dorsal,
	];

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var mountedOn in MountedOn)
		{
			var action = Bind(actorId, mountedOn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public TorpedoAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		new(actorId, mountedOn);

	IAction IMountedActionDef.Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn);

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
		var (position, _, _) = TorpedoMount.LaunchPose(ship, action.MountedOn);
		return world.Grid.IsInBounds(position) && !world.BlockedFor(action.ActorId).Contains(position);
	}

	public bool IsLegal(TorpedoAction action, BattleWorld world, ActorRuntime runtime)
	{
		var firer = UnitRegistry.For(world).UnitOf(action.ActorId);
		if (firer.Alliance.Team != ETeam.Player)
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
		new SpawnTorpedoEffect(action.MountedOn, action.SpawnedUnitId),
		new TorpedoCooldownEffect(TorpedoConfig.CooldownTurns),
	];

	private static TorpedoAction Cast(IAction action) =>
		action as TorpedoAction ?? throw new ArgumentException($"Expected {nameof(TorpedoAction)}.", nameof(action));
}
