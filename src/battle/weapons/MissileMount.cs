namespace GrimSpace.Battle.Weapons;

public enum EMissileMount
{
	Fore,
}

public sealed class MissileMountConfig
{
	public required int Range { get; init; }
	public required int MinFore { get; init; }
	public required int MaxAbsPort { get; init; }
	public required int MinDorsal { get; init; }
	public required int MaxDorsal { get; init; }

	public static MissileMountConfig For(EMissileMount mount) =>
		mount switch
		{
			EMissileMount.Fore => CombatConfig.ForeMissile,
			_ => throw new ArgumentOutOfRangeException(nameof(mount)),
		};

	public MissileMountConfig WithRange(int range) =>
		new()
		{
			Range = range,
			MinFore = MinFore,
			MaxAbsPort = MaxAbsPort,
			MinDorsal = MinDorsal,
			MaxDorsal = MaxDorsal,
		};
}
