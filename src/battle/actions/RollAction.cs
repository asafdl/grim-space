using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RollAction(
	string OwnerId,
	ERollDirection Direction,
	int? UndoGroup = null) : IAction;
