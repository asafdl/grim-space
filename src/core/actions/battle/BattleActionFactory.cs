namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Stamps <see cref="IAction.OwnerId"/> on battle action prototypes used for legality probes and AI candidates.
/// </summary>
public static class BattleActionFactory
{
	public static IBattleAction WithOwner(string ownerId, IBattleAction action) => action switch
	{
		MoveAction move => new MoveAction(ownerId, move.Option),
		HeadingTurnAction heading => new HeadingTurnAction(ownerId, heading.Turn),
		RollAction roll => new RollAction(ownerId, roll.Direction),
		MissileAction missile => new MissileAction(ownerId, missile.Center, missile.Mount, missile.Range),
		RailgunAction railgun => new RailgunAction(ownerId, railgun.TargetUnitId),
		_ => action,
	};

	public static IAction AsQueued(string ownerId, IBattleAction action) =>
		(IAction)WithOwner(ownerId, action);
}
