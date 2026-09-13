using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public enum EAbilitySourceVisual
{
	FlakBurst,
	Railgun,
	Torpedo,
	Patrol,
	Detonate,
}

public sealed record AbilityActivationChoice(
	IAction Action,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	EAbilitySourceVisual Visual,
	ESpatialOrientation? MountedOn = null);

public sealed class AbilityActivation
{
	private readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> _def;

	private AbilityActivation(
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def) =>
		_def = def;

	public IReadOnlyList<AbilityActivationChoice> ResolveChoices(
		State actor,
		IEnumerable<IAction> legalCapabilities) =>
		legalCapabilities
			.Where(IsForDefinition)
			.Select(action => ResolveChoice(actor, action))
			.ToList();

	public static AbilityActivation For(
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def) =>
		def switch
		{
			IMountedActionDef or IActorActionDef => new AbilityActivation(def),
			_ => throw new NotSupportedException(
				$"Ability activation is not supported for {def.GetType().Name}."),
		};

	public static IAction CreateExecutionAction(AbilityActivationChoice choice) =>
		choice.Action switch
		{
			IMountedAction mounted
				when choice.Action is IAction<BattleWorld, ActorRuntime>
				{
					Definition: IMountedActionDef def,
				} => def.Bind(choice.Action.ActorId, mounted.MountedOn),
			IAction<BattleWorld, ActorRuntime>
				{
					Definition: IActorActionDef def,
				} => def.Bind(choice.Action.ActorId),
			_ => throw new NotSupportedException(
				$"Ability execution is not supported for {choice.Action.GetType().Name}."),
		};

	private bool IsForDefinition(IAction action) =>
		action is IAction<BattleWorld, ActorRuntime> typed
		&& ReferenceEquals(typed.Definition, _def);

	private static AbilityActivationChoice ResolveChoice(State actor, IAction action)
	{
		var frame = BodyFrame.From(actor);
		return action switch
		{
			FlakAction flak => MountedChoice(
				action,
				actor.Position + frame.Step(flak.MountedOn),
				frame.Step(flak.MountedOn),
				actor.Dorsal,
				EAbilitySourceVisual.FlakBurst,
				flak.MountedOn),
			TorpedoAction torpedo => MountedChoice(
				action,
				TorpedoMount.LaunchPose(actor, torpedo.MountedOn),
				EAbilitySourceVisual.Torpedo,
				torpedo.MountedOn),
			RailgunAction => MountedChoice(
				action,
				actor.Position + actor.Fore,
				actor.Fore,
				actor.Dorsal,
				EAbilitySourceVisual.Railgun),
			SpawnPatrolAction => MountedChoice(
				action,
				PatrolBayMount.LaunchPose(actor),
				EAbilitySourceVisual.Patrol),
			DetonateAction => MountedChoice(
				action,
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				EAbilitySourceVisual.Detonate),
			_ => throw new NotSupportedException(
				$"Ability source is not supported for {action.GetType().Name}."),
		};
	}

	private static AbilityActivationChoice MountedChoice(
		IAction action,
		(Coord Position, Coord Fore, Coord Dorsal) pose,
		EAbilitySourceVisual visual,
		ESpatialOrientation? mountedOn = null) =>
		MountedChoice(action, pose.Position, pose.Fore, pose.Dorsal, visual, mountedOn);

	private static AbilityActivationChoice MountedChoice(
		IAction action,
		Coord position,
		Coord fore,
		Coord dorsal,
		EAbilitySourceVisual visual,
		ESpatialOrientation? mountedOn = null) =>
		new(action, position, fore, dorsal, visual, mountedOn);
}
