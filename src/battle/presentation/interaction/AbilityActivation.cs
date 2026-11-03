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
	ESpatialOrientation? Orientation = null);

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
					pose.Orientation);
			})
			.ToList();

	public static AbilityActivationChoice? ImmediateChoice(
		IReadOnlyList<AbilityActivationChoice> choices) =>
		choices.Count == 1 ? choices[0] : null;

	public static int? IndexOfOrientedChoice(
		IReadOnlyList<AbilityActivationChoice> choices,
		ESpatialOrientation face)
	{
		for (var i = 0; i < choices.Count; i++)
		{
			if (choices[i].Orientation == face)
				return i;
		}

		return null;
	}

	public static IAction CreateExecutionAction(AbilityActivationChoice choice) =>
		choice.Action switch
		{
			RoadsWeDontNeedRoadsAction jump => jump,
			VoidBombAction torpedo when torpedo.SpawnedUnitId == Capabilities.PreviewVoidBombId =>
				VoidBombDef.Instance.Bind(torpedo.ActorId, torpedo.MountedOn),
			VoidBombAction torpedo =>
				VoidBombDef.Instance.Bind(torpedo.ActorId, torpedo.MountedOn, torpedo.SpawnedUnitId),
			SpawnRepurposedMinerAction repurposedMiner when repurposedMiner.SpawnedUnitId == Capabilities.PreviewRepurposedMinerId =>
				SpawnRepurposedMinerDef.Instance.Bind(repurposedMiner.ActorId, repurposedMiner.MountedOn),
			SpawnRepurposedMinerAction repurposedMiner =>
				new SpawnRepurposedMinerAction(repurposedMiner.ActorId, repurposedMiner.MountedOn, repurposedMiner.SpawnedUnitId),
			GoopGunAction goopGun when goopGun.GoopHazardId == Capabilities.PreviewGoopHazardId =>
				GoopGunDef.Instance.Bind(goopGun.ActorId, goopGun.MountedOn),
			GoopGunAction goopGun =>
				GoopGunDef.Instance.Bind(goopGun.ActorId, goopGun.MountedOn, goopGun.GoopHazardId),
			ITargetedAction and IOrientedAction => choice.Action,
			IOrientedAction oriented
				when choice.Action is IAction<BattleWorld, ActorRuntime>
				{
					Definition: IOrientedActionDef def,
				} => def.Bind(choice.Action.ActorId, oriented.Orientation),
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
