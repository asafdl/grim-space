using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units;

public sealed record ShipSnapshot(
	string Id,
	ShipConfiguration Configuration,
	int HullPoints,
	FaceShieldPoints ShieldPoints)
{
	public static ShipSnapshot FromConfiguration(string id, ShipConfiguration configuration) =>
		new(
			id,
			configuration,
			configuration.MaxHullPoints,
			configuration.Defenses.Clone());
}
