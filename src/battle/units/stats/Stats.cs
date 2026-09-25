using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Specs;

namespace GrimSpace.Battle.Units;

public sealed class Stats
{
	public int MaxAp { get; init; }

	public static Stats ForLoadout(ShipSpec chassis, ShipLoadout loadout) =>
		ForChassis(chassis);

	public static Stats ForChassis(ShipSpec chassis) => ForType(chassis.Chassis);

	public static Stats ForType(EType type) =>
		type switch
		{
			EType.Fighter => new Stats { MaxAp = 4 },
			EType.Carrier => new Stats { MaxAp = 3 },
			EType.Patrol => new Stats { MaxAp = 4 },
			EType.Torpedo => new Stats { MaxAp = TorpedoSpec.MovementActionPoints },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
