using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record LightningCannonAction(
	string ActorId,
	ESpatialOrientation MountedOn = ESpatialOrientation.Forward)
	: IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		LightningCannonDef.Instance;
}

public sealed class LightningCannonDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef,
		IAreaActionDef
{
	public static LightningCannonDef Instance { get; } = new();

	public bool IsBlockable => true;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var installed in world.StateOf(actorId).Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.LightningCannon)
				continue;

			var action = Bind(actorId, installed.MountedOn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public LightningCannonAction Bind(
		string actorId,
		ESpatialOrientation mountedOn = ESpatialOrientation.Forward) =>
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

	public bool IsPossible(LightningCannonAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).FindInstalled(EAbilityKind.LightningCannon, action.MountedOn) is not null;

	public bool IsLegal(LightningCannonAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.LightningCannon, action.MountedOn);
		if (installed is null || state.MountRuntimeFor(installed.Mount).UsesRemaining <= 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		LightningCannonAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var cells = AffectedCells(action, world);

		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.LightningCannon, action.MountedOn)
			?? throw new InvalidOperationException("Lightning cannon ability not installed for actor.");
		var damage = installed.Spec is IAreaDamage area ? area.Damage : throw new InvalidOperationException("Lightning cannon spec missing area damage.");
		return
		[
			new ApplyAreaDamageEffect(
				EImpactCause.LightningCannonBurst,
				cells,
				damage,
				state.Position),
			new MountUsesChangeEffect(installed.Mount, -1),
		];
	}

	public HashSet<Coord> AffectedCells(LightningCannonAction action, BattleWorld world)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.LightningCannon, action.MountedOn);
		if (installed?.Spec is not IAreaDamage areaDamage)
			return [];

		var frame = BodyFrame.From(state);
		var geometric = AbilityArea.CellsInBounds(areaDamage, frame, action.MountedOn, world.Grid);
		return AbilityArea.ApplyBlocking(state.Position, geometric, IsBlockable, world.Hazards);
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	private static LightningCannonAction Cast(IAction action) =>
		action as LightningCannonAction ?? throw new ArgumentException($"Expected {nameof(LightningCannonAction)}.", nameof(action));
}
