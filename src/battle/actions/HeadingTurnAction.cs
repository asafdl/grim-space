using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record HeadingTurnAction(
	string OwnerId,
	EHeadingTurn Turn,
	int? UndoGroup = null) : IAction;
