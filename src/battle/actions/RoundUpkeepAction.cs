using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RoundUpkeepAction(
	string OwnerId,
	int? UndoGroup = null) : IAction;
