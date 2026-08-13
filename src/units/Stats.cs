using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public sealed class Stats
{
	public int MaxAp { get; init; }
	public int MaxHullPoints { get; init; }
	public FaceShieldPoints MaxShieldPoints { get; init; } = new();
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
				MaxShieldPoints = FaceShieldPoints.MaxFor(EType.Fighter),
				FlaksPerTurn = 1,
				RailgunsPerTurn = 1,
				MinPathApCost = 0,
			},
			EType.Carrier => new Stats
			{
				MaxAp = 3,
				MaxHullPoints = 2,
				MaxShieldPoints = FaceShieldPoints.MaxFor(EType.Carrier),
				FlaksPerTurn = 0,
				RailgunsPerTurn = 1,
				MinPathApCost = 0,
			},
			EType.Patrol => new Stats
			{
				MaxAp = 4,
				MaxHullPoints = 1,
				MaxShieldPoints = FaceShieldPoints.MaxFor(EType.Patrol),
				FlaksPerTurn = 1,
				RailgunsPerTurn = 0,
				MinPathApCost = 0,
			},
			EType.Torpedo => new Stats
			{
				MaxAp = 3,
				MaxHullPoints = 1,
				MaxShieldPoints = FaceShieldPoints.MaxFor(EType.Torpedo),
				FlaksPerTurn = 0,
				RailgunsPerTurn = 0,
				MinPathApCost = 1,
			},
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
