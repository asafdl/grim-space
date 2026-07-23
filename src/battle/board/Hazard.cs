using GrimSpace.Core;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Board;

public sealed class Hazard : NonUnit
{
	public const int AsteroidBlockPadding = 1;

	public required Coord Center { get; init; }
	public required bool Passable { get; init; }
	public required int Damage { get; init; }
	public required int MomentumLoss { get; init; }
	public required EHazardKind Kind { get; init; }
	public int Radius { get; init; }
	public string? VisualId { get; init; }

	public static Hazard MissileZone(
		string id,
		string actorId,
		Coord center,
		BodyFrame ownerFrame,
		BoundedGrid grid,
		int radius,
		int damage,
		int momentumLoss) =>
		new()
		{
			Id = id,
			ActorId = actorId,
			Center = center,
			Frame = ownerFrame with { Origin = center },
			Cells = new HashSet<Coord>(grid.EnumerateCube(center, radius)),
			Passable = true,
			Damage = damage,
			MomentumLoss = momentumLoss,
			Kind = EHazardKind.MissileZone,
		};

	public static Hazard FlakBurst(
		string id,
		string actorId,
		BodyFrame ownerFrame,
		IEnumerable<Coord> cells) =>
		new()
		{
			Id = id,
			ActorId = actorId,
			Center = ownerFrame.Origin,
			Frame = ownerFrame,
			Cells = new HashSet<Coord>(cells),
			Passable = true,
			Damage = 0,
			MomentumLoss = CombatConfig.FlakMomentumLoss,
			Kind = EHazardKind.FlakBurst,
		};

	public static int BlockRadiusFor(int radius) => radius + AsteroidBlockPadding;

	public static Hazard Asteroid(
		string id,
		Coord center,
		BoundedGrid grid,
		int radius,
		string visualId) =>
		new()
		{
			Id = id,
			ActorId = EntityIds.World,
			Center = center,
			Frame = BodyFrame.WorldAligned(center),
			Cells = new HashSet<Coord>(grid.EnumerateCube(center, BlockRadiusFor(radius))),
			Passable = false,
			Damage = 0,
			MomentumLoss = 0,
			Kind = EHazardKind.MissileZone,
			Radius = radius,
			VisualId = visualId,
		};

	public Hazard Clone() =>
		new()
		{
			Id = Id,
			ActorId = ActorId,
			Center = Center,
			Frame = Frame,
			Cells = new HashSet<Coord>(Cells),
			Passable = Passable,
			Damage = Damage,
			MomentumLoss = MomentumLoss,
			Kind = Kind,
			Radius = Radius,
			VisualId = VisualId,
		};
}
