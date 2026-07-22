using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record MissileAction(
	string OwnerId,
	Coord Center,
	EMissileMount Mount,
	int Range,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => MissileDef.For(Mount, Range);
}
