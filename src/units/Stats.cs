using System;
using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public sealed class Stats
{
	public int MaxAp { get; init; }
	public int MaxHp { get; init; }

	public static Stats ForType(EType type) =>
		type switch
		{
			EType.Fighter => new Stats { MaxAp = 4, MaxHp = 1 },
			_ => throw new ArgumentOutOfRangeException(nameof(type)),
		};
}
