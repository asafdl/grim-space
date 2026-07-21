using GrimSpace.Core;

namespace GrimSpace.Core.Actions;

public static class SystemAction
{
	public static bool Is(IAction action) => Is(action.OwnerId);

	public static bool Is(string ownerId) => ownerId == EntityIds.System;
}
