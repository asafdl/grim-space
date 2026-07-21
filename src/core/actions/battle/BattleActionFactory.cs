using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Stamps <see cref="IAction.OwnerId"/> on action prototypes used for legality probes and AI candidates.
/// </summary>
public static class BattleActionFactory
{
	public static IAction WithOwner(string ownerId, IAction action) => action switch
	{
		MoveStepAction step => new MoveStepAction(
			ownerId,
			step.From,
			step.To,
			step.UsedDirectionsMaskBefore,
			step.UndoGroup),
		HeadingTurnAction heading => new HeadingTurnAction(ownerId, heading.Turn, heading.UndoGroup),
		RollAction roll => new RollAction(ownerId, roll.Direction, roll.UndoGroup),
		MissileAction missile => new MissileAction(
			ownerId,
			missile.Center,
			missile.Mount,
			missile.Range,
			missile.UndoGroup),
		FlakAction flak => new FlakAction(ownerId, flak.Mount, flak.UndoGroup),
		RailgunAction railgun => new RailgunAction(ownerId, railgun.TargetUnitId, railgun.UndoGroup),
		_ => action,
	};
}
