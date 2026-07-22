using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record MissileAction(
	string OwnerId,
	Coord Center,
	EMissileMount Mount,
	int Range,
	int? UndoGroup = null) : IAction;
