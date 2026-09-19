namespace GrimSpace.Units;

public sealed record FleetMember(string Id)
{
	public static FleetMember ForShip(string shipId) => new(shipId);
}
