using GrimSpace.Battle.NonUnits;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Encounter;

public sealed class BattleHazardSpawn
{
	public EBattleHazardKind Kind { get; init; } = EBattleHazardKind.Asteroid;
	public required Coord Origin { get; init; }
	public required IReadOnlySet<Coord> Cells { get; init; }

	public NonUnit CreateNonUnit(BoundedGrid grid) =>
		Kind switch
		{
			EBattleHazardKind.Asteroid => Asteroid.Create(
				TypedIdGenerator.NextId(NonUnitTypeSlug.Asteroid),
				Origin,
				grid,
				Cells),
			EBattleHazardKind.CryoPlume => CryoPlume.Create(
				TypedIdGenerator.NextId(NonUnitTypeSlug.CryoPlume),
				Origin,
				grid),
			_ => throw new InvalidOperationException($"Unsupported hazard kind '{Kind}'."),
		};
}
