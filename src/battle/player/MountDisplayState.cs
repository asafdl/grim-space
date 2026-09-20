using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Player;

public sealed record MountDisplayState(
	EAbilityKind Kind,
	int UsesRemaining,
	int UsesPerTurn,
	int CooldownRemaining)
{
	public static MountDisplayState Capture(InstalledAbility installed, MountRuntimeCounters runtime)
	{
		var usesPerTurn = installed.Spec is IPerTurnAbility perTurn ? perTurn.UsesPerTurn : 0;
		return new(
			installed.Spec.Kind,
			runtime.UsesRemaining,
			usesPerTurn,
			runtime.CooldownRemaining);
	}
}
