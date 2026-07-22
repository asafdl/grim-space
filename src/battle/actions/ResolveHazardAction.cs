using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ResolveHazardAction(
	string OwnerId,
	string HazardId,
	int? UndoGroup = null) : IAction;
