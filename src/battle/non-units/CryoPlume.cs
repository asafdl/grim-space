using System.Collections.Frozen;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.NonUnits;

/// <summary>Cold volatile jet venting from an icy fragment; ships dump radiator heat into it.</summary>
public sealed class CryoPlume : Hazard
{
	public const int HeatCleared = 20;

	public override IEnumerable<Coord> MovementBlockingCells => [];
	public override IEnumerable<Coord> AbilityBlockingCells => [];

	public static CryoPlume Create(string id, Coord cell, BoundedGrid grid)
	{
		if (!grid.IsInBounds(cell))
			throw new ArgumentOutOfRangeException(nameof(cell), "Cryo plume must be inside the battle grid.");

		return new CryoPlume
		{
			Id = id,
			ActorId = BattleActorIds.Terrain,
			Center = cell,
			Frame = BodyFrame.WorldAligned(cell),
			Cells = new[] { cell }.ToFrozenSet(),
		};
	}
}
