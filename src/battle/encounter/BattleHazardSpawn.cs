using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Encounter;

public sealed class BattleHazardSpawn
{
	public EBattleHazardKind Kind { get; init; } = EBattleHazardKind.Asteroid;
	public required Coord Origin { get; init; }
	public required IReadOnlySet<Coord> Cells { get; init; }
	public BodyFrame? Frame { get; init; }
	public int? DamageResistancePercent { get; init; }

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
			EBattleHazardKind.IonStorm => IonStorm.Create(
				TypedIdGenerator.NextId(NonUnitTypeSlug.IonStorm),
				Origin,
				grid),
			EBattleHazardKind.GravityWell => GravityWell.Create(
				TypedIdGenerator.NextId(NonUnitTypeSlug.GravityWell),
				Origin,
				Frame ?? throw new InvalidOperationException("Gravity well spawn requires a frame."),
				grid,
				Cells),
			EBattleHazardKind.AsteroidBelt => CreateAsteroidBelt(grid),
			_ => throw new InvalidOperationException($"Unsupported hazard kind '{Kind}'."),
		};

	private AsteroidBelt CreateAsteroidBelt(BoundedGrid grid)
	{
		if (!Cells.SetEquals([Origin]))
			throw new ArgumentException("Asteroid belt cover must occupy exactly its origin cell.", nameof(Cells));
		if (DamageResistancePercent is not { } resistancePercent)
			throw new InvalidOperationException("Asteroid belt spawn requires damage resistance.");

		return AsteroidBelt.Create(
			TypedIdGenerator.NextId(NonUnitTypeSlug.AsteroidBelt),
			Origin,
			grid,
			resistancePercent);
	}
}
