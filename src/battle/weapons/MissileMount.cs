namespace GrimSpace.Battle.Weapons;

public enum EMissileMount
{
	Dorsal,
}

public sealed class MissileMountConfig
{
	public required int Range { get; init; }
	public required int MinForward { get; init; }
	public required int MaxAbsRight { get; init; }
	public required int MinUp { get; init; }
	public required int MaxUp { get; init; }

	public static MissileMountConfig For(EMissileMount mount) =>
		mount switch
		{
			EMissileMount.Dorsal => CombatConfig.DorsalMissile,
			_ => throw new ArgumentOutOfRangeException(nameof(mount)),
		};

	public MissileMountConfig WithRange(int range) =>
		new()
		{
			Range = range,
			MinForward = MinForward,
			MaxAbsRight = MaxAbsRight,
			MinUp = MinUp,
			MaxUp = MaxUp,
		};
}
