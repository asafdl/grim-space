using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public sealed record AbilityActivationChoice(
	IAction Action,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	AbilityTargetingSpec Targeting,
	ESpatialOrientation? MountedOn = null);

public static class AbilityActivation
{
	public static IReadOnlyList<AbilityActivationChoice> ResolveChoices(
		AbilityHudCatalog.Spec spec,
		State actor,
		IEnumerable<IAction> legalCapabilities) =>
		legalCapabilities
			.Where(action => IsForDefinition(action, spec.Def))
			.Select(action =>
			{
				var pose = spec.Targeting.ResolveSource(actor, action);
				return new AbilityActivationChoice(
					action,
					pose.Position,
					pose.Fore,
					pose.Dorsal,
					spec.Targeting,
					pose.MountedOn);
			})
			.ToList();

	public static IAction CreateExecutionAction(AbilityActivationChoice choice) =>
		choice.Action switch
		{
			TorpedoAction torpedo when torpedo.SpawnedUnitId == Capabilities.PreviewTorpedoId =>
				TorpedoDef.Instance.Bind(torpedo.ActorId, torpedo.MountedOn),
			TorpedoAction torpedo =>
				TorpedoDef.Instance.Bind(torpedo.ActorId, torpedo.MountedOn, torpedo.SpawnedUnitId),
			SpawnPatrolAction patrol when patrol.SpawnedUnitId == Capabilities.PreviewPatrolId =>
				SpawnPatrolDef.Instance.Bind(patrol.ActorId, patrol.MountedOn),
			SpawnPatrolAction patrol =>
				new SpawnPatrolAction(patrol.ActorId, patrol.MountedOn, patrol.SpawnedUnitId),
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

	private static bool IsForDefinition(
		IAction action,
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def) =>
		action is IAction<BattleWorld, ActorRuntime> typed
		&& ReferenceEquals(typed.Definition, def);
}
