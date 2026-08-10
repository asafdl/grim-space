using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public sealed class Stats
{
	public int MaxAp { get; init; }
	public int MaxHullPoints { get; init; }
	public int MaxShieldPointsPerFace { get; init; }
	public int FlaksPerTurn { get; init; }
	public int RailgunsPerTurn { get; init; }
	public int MinPathApCost { get; init; }

	public static Stats ForType(EType type) =>
		type switch
		{
			EType.Fighter => new Stats
			{
				MaxAp = 4,
				MaxHullPoints = 2,
				MaxShieldPointsPerFace = 2,
				FlaksPerTurn = 1,
				RailgunsPerTurn = 1,
				MinPathApCost = 0,
			},
			EType.Patrol => new Stats
			{
				MaxAp = 4,
				MaxHullPoints = 2,
				MaxShieldPointsPerFace = 2,
				FlaksPerTurn = 0,
				RailgunsPerTurn = 1,
				MinPathApCost = 0,
			},
			EType.Torpedo => new Stats
			{
				MaxAp = 3,
				MaxHullPoints = 1,
				MaxShieldPointsPerFace = 0,
				FlaksPerTurn = 0,
				RailgunsPerTurn = 0,
				MinPathApCost = 1,
			},
			_ => throw new ArgumentOutOfRangeException(nameof(type)),
		};
}
