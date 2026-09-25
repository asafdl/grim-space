using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units.Specs;

public sealed class TorpedoSpec : ShipSpec
{
	internal const int FuelTurns = 3;
	internal const int MovementActionPoints = 4;
	internal const int ForwardMoveApCost = 1;
	internal const int LateralMoveApCost = 2;
	internal const int BlastRadius = 4;
	internal const int BlastDamage = 3;

	public static TorpedoSpec Instance { get; } = new();

	private TorpedoSpec()
	{
	}

	public override EType Chassis => EType.Torpedo;

	public override int DefaultMaxHullPoints => 1;

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile.Fill(1);
			profile[ESpatialOrientation.Retro] = 0;
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } = [];
}
