using GrimSpace.Core;

namespace GrimSpace.Core.Actions;

public static class SystemAction
{
	public static bool Is(IAction action) => Is(action.ActorId);

	public static bool Is(string actorId) => actorId == EntityIds.System;
}
