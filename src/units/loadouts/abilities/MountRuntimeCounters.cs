namespace GrimSpace.Units.Loadouts.Abilities;

public sealed class MountRuntimeCounters
{
	public int UsesRemaining { get; set; }
	public int CooldownRemaining { get; set; }

	public MountRuntimeCounters Clone() =>
		new()
		{
			UsesRemaining = UsesRemaining,
			CooldownRemaining = CooldownRemaining,
		};
}
