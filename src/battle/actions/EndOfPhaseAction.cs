using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record EndOfPhaseAction(
	string OwnerId,
	int? UndoGroup = null) : IAction;
