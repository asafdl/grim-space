using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Stamps <see cref="IAction.OwnerId"/> on action prototypes used for legality probes and AI candidates.
/// </summary>
public static class BattleActionFactory
{
	public static IAction WithOwner(string ownerId, IAction action) => action switch
	{
		MoveStepAction move => move with { OwnerId = ownerId },
		HeadingTurnAction heading => heading with { OwnerId = ownerId },
		RollAction roll => roll with { OwnerId = ownerId },
		MissileAction missile => missile with { OwnerId = ownerId },
		FlakAction flak => flak with { OwnerId = ownerId },
		RailgunAction railgun => railgun with { OwnerId = ownerId },
		EndOfPhaseAction end => end with { OwnerId = ownerId },
		RoundUpkeepAction upkeep => upkeep with { OwnerId = ownerId },
		ResolveHazardAction resolve => resolve with { OwnerId = ownerId },
		_ => action,
	};
}
