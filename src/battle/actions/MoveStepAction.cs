using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record MoveStepAction(
	string OwnerId,
	EStepDirection Direction,
	int? UndoGroup = null) : IAction;
