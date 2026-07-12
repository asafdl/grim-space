using System;
using GrimSpace.Domain.Units.Enums;

namespace GrimSpace.Domain.Units;

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
