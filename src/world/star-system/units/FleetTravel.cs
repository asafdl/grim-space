using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Units;

public abstract record FleetTravel
{
	public sealed record AtRest(Coord Position) : FleetTravel;

	public sealed record Journey(
		long Id,
		Coord Origin,
		Coord Destination,
		int StartTick) : FleetTravel;
}
