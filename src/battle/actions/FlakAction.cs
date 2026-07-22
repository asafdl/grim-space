using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record FlakAction(
	string OwnerId,
	EFlakMount Mount,
	int? UndoGroup = null) : IAction;
