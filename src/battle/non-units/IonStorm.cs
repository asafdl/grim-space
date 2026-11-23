using System.Collections.Frozen;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.NonUnits;

public sealed class IonStorm : Hazard
{
	public const int ShieldPercentOfFaceMax = 25;
	public const string ShieldGrantId = "IonStorm";

	public override IEnumerable<Coord> MovementBlockingCells => [];
	public override IEnumerable<Coord> AbilityBlockingCells => [];

	public static IonStorm Create(string id, Coord cell, BoundedGrid grid)
	{
		if (!grid.IsInBounds(cell))
			throw new ArgumentOutOfRangeException(nameof(cell), "Ion storm must be inside the battle grid.");

		return new IonStorm
		{
			Id = id,
			ActorId = BattleActorIds.Terrain,
			Center = cell,
			Frame = BodyFrame.WorldAligned(cell),
			Cells = new[] { cell }.ToFrozenSet(),
		};
	}
}
