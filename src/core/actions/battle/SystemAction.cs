using GrimSpace.Battle.Ids;
using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Actions.Battle;

public static class SystemAction
{
	public static bool Is(IAction action) => Is(action.OwnerId);

	public static bool Is(string ownerId) => ownerId == EntityIds.System;
}
