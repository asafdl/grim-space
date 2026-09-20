using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class Stats
{
	public int MaxAp { get; init; }

	public static Stats ForSpec(ShipSpec spec) =>
		spec.Chassis switch
		{
			EType.Torpedo => new Stats { MaxAp = TorpedoBodySpec.Require(spec).MovementActionPoints },
			_ => ForType(spec.Chassis),
		};

	public static Stats ForType(EType type) =>
		type switch
		{
			EType.Fighter => new Stats { MaxAp = 4 },
			EType.Carrier => new Stats { MaxAp = 3 },
			EType.Patrol => new Stats { MaxAp = 4 },
			EType.Torpedo => ForSpec(ShipCatalog.DefaultFor(EType.Torpedo)),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
