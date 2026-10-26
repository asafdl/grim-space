using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Units.Specs;

public sealed class VoidBombSpec : ShipSpec
{
	internal const int FuelTurns = 2;
	internal const int MovementActionPoints = 4;
	internal const int ForwardMoveApCost = 1;
	internal const int LateralMoveApCost = 2;
	internal const int BlastRadius = 3;
	internal const int BlastDamage = 100;

	public static VoidBombSpec Instance { get; } = new();

	private VoidBombSpec()
	{
	}

	public override EType Chassis => EType.VoidBomb;
	public override int Initiative => 10;

	public override int DefaultMaxHullPoints => 100;
	public override ManeuverabilitySpec Maneuverability { get; } =
		ManeuverabilitySpec.VoidBomb(
			MovementActionPoints,
			ForwardMoveApCost,
			LateralMoveApCost);

	public override HeatSpec Heat => HeatSpec.None;

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile.Fill(100);
			profile[ESpatialOrientation.Retro] = 0;
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } = [];
}
