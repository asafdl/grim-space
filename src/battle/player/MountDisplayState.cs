using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Player;

public sealed record MountDisplayState(
	AbilityMount Mount,
	int UsesRemaining,
	int UsesPerTurn,
	int CooldownRemaining)
{
	public static MountDisplayState Capture(InstalledAbility installed, MountRuntimeCounters runtime)
	{
		var usesPerTurn = installed.Spec is IPerTurnAbility perTurn ? perTurn.UsesPerTurn : 0;
		return new(
			installed.Mount,
			runtime.UsesRemaining,
			usesPerTurn,
			runtime.CooldownRemaining);
	}
}
