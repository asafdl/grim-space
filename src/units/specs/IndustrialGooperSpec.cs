using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Units.Specs;

public sealed class IndustrialGooperSpec : ShipSpec
{
	public static IndustrialGooperSpec Instance { get; } = new();

	private IndustrialGooperSpec()
	{
	}

	public override EType Chassis => EType.IndustrialGooper;
	public override int Initiative => 25;
	public override int DefaultMaxHullPoints => 3;
	public override ManeuverabilitySpec Maneuverability { get; } =
		ManeuverabilitySpec.StandardShip(4, 3);

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile.Fill(3);
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } =
	[
		new(new(EAbilityKind.GoopGun, ESpatialOrientation.Forward)),
		new(new(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Retro)),
	];
}
