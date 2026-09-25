namespace GrimSpace.Units;

/// <summary>
/// Read-only registry access for validating merchant purchase snapshots without exposing mutation.
/// </summary>
public interface IShipRegistryReader
{
	bool Matches(string shipId, ShipInstance expected);
}
