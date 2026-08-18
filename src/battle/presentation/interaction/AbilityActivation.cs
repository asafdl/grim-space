using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public abstract class AbilityActivation
{
	public abstract string WaitingLabel { get; }
	public string ConfirmLabel => BattleHudCopy.ConfirmAction;

	public abstract bool CanConfirm(ESpatialOrientation? stagedMountedOn);

	public abstract IAction? Build(string actorId, ESpatialOrientation? stagedMountedOn);

	public ActionInstruction ResolveInstruction(bool visible, ESpatialOrientation? stagedMountedOn)
	{
		if (!visible)
			return default;

		var canConfirm = CanConfirm(stagedMountedOn);
		var label = canConfirm ? ConfirmLabel : WaitingLabel;
		return new ActionInstruction(Visible: true, Label: label, CanConfirm: canConfirm);
	}

	public static AbilityActivation For(
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def) =>
		def switch
		{
			IMountedActionDef mounted => new MountedActivation(mounted),
			IActorActionDef actorOnly => new ActorOnlyActivation(actorOnly),
			_ => throw new NotSupportedException(
				$"Ability activation is not supported for {def.GetType().Name}."),
		};

	private sealed class ActorOnlyActivation(IActorActionDef def) : AbilityActivation
	{
		public override string WaitingLabel => ConfirmLabel;

		public override bool CanConfirm(ESpatialOrientation? stagedMountedOn) => true;

		public override IAction? Build(string actorId, ESpatialOrientation? stagedMountedOn) =>
			def.Bind(actorId);
	}

	private sealed class MountedActivation(IMountedActionDef def) : AbilityActivation
	{
		public override string WaitingLabel => BattleHudCopy.SelectFiringDirection;

		public override bool CanConfirm(ESpatialOrientation? stagedMountedOn) =>
			stagedMountedOn is not null;

		public override IAction? Build(string actorId, ESpatialOrientation? stagedMountedOn) =>
			stagedMountedOn is ESpatialOrientation mountedOn
				? def.Bind(actorId, mountedOn)
				: null;
	}
}
