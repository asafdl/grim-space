using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RailgunAction(
	string OwnerId,
	string TargetUnitId,
	int? UndoGroup = null) : IAction;
