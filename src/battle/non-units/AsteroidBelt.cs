using System.Collections.Frozen;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.NonUnits;

public sealed class AsteroidBelt : Hazard
{
	public const int LowResistancePercent = 40;
	public const int HighResistancePercent = 80;
	public const int ResistanceLostPerHit = 40;
	public const int OutgoingDamageRetainedPercent = 30;

	public required int DamageResistancePercent { get; init; }

	public override IEnumerable<Coord> MovementBlockingCells => [];
	public override IEnumerable<Coord> AbilityBlockingCells => [];

	public static AsteroidBelt Create(
		string id,
		Coord cell,
		BoundedGrid grid,
		int damageResistancePercent)
	{
		if (!grid.IsInBounds(cell))
			throw new ArgumentOutOfRangeException(nameof(cell), "Asteroid belt cover must be inside the battle grid.");
		if (damageResistancePercent is not (LowResistancePercent or HighResistancePercent))
			throw new ArgumentOutOfRangeException(nameof(damageResistancePercent));

		return new AsteroidBelt
		{
			Id = id,
			ActorId = BattleActorIds.Terrain,
			Center = cell,
			Frame = BodyFrame.WorldAligned(cell),
			Cells = new[] { cell }.ToFrozenSet(),
			DamageResistancePercent = damageResistancePercent,
		};
	}
}
