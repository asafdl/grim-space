namespace GrimSpace.Battle.Abilities;

using GrimSpace.Math.Grid;

public static class TorpedoConfig
{
	public const int MovementActionPoints = 4;
	public const int ForwardMoveApCost = 1;
	public const int LateralMoveApCost = 2;
	public const int Fuel = 3;
	public const int CooldownTurns = 3;
	public const int BlastRadius = 4;
	public const int BlastDamage = 3;

	public static int? MoveApCost(ESpatialOrientation direction) =>
		direction switch
		{
			ESpatialOrientation.Forward => ForwardMoveApCost,
			ESpatialOrientation.Port
				or ESpatialOrientation.Starboard
				or ESpatialOrientation.Dorsal
				or ESpatialOrientation.Ventral => LateralMoveApCost,
			_ => null,
		};
}
