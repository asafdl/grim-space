using GrimSpace.Math.Grid;

namespace GrimSpace.Units;

public sealed record TorpedoBodySpec(
	int FuelTurns,
	int MovementActionPoints,
	int ForwardMoveApCost,
	int LateralMoveApCost,
	int BlastRadius,
	int BlastDamage)
{
	public int? MoveApCost(ESpatialOrientation direction) =>
		direction switch
		{
			ESpatialOrientation.Forward => ForwardMoveApCost,
			ESpatialOrientation.Port
				or ESpatialOrientation.Starboard
				or ESpatialOrientation.Dorsal
				or ESpatialOrientation.Ventral => LateralMoveApCost,
			_ => null,
		};

	public static TorpedoBodySpec Require(ShipSpec spec) =>
		spec.TorpedoBody
		?? throw new InvalidOperationException(
			$"Ship spec for chassis '{spec.Chassis}' has no torpedo body configuration.");
}
