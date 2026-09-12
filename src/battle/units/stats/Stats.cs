using GrimSpace.Battle.Abilities;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class Stats
{
	public int MaxAp { get; init; }
	public int MaxHullPoints { get; init; }
	public FaceShieldPoints MaxShieldPoints { get; init; } = new();
	public int FlaksPerTurn { get; init; }
	public int RailgunsPerTurn { get; init; }

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
			},
			EType.Carrier => new Stats
			{
				MaxAp = 3,
				MaxHullPoints = 2,
				MaxShieldPoints = FaceShieldPoints.MaxFor(EType.Carrier),
				FlaksPerTurn = 0,
				RailgunsPerTurn = 1,
			},
			EType.Patrol => new Stats
			{
				MaxAp = 4,
				MaxHullPoints = 1,
				MaxShieldPoints = FaceShieldPoints.MaxFor(EType.Patrol),
				FlaksPerTurn = 1,
				RailgunsPerTurn = 0,
			},
			EType.Torpedo => new Stats
			{
				MaxAp = TorpedoConfig.MovementActionPoints,
				MaxHullPoints = 1,
				MaxShieldPoints = FaceShieldPoints.MaxFor(EType.Torpedo),
				FlaksPerTurn = 0,
				RailgunsPerTurn = 0,
			},
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
