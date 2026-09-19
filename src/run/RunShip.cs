using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Run;

public sealed class RunShip
{
	public string Id { get; }
	public ShipConfiguration Configuration { get; set; }
	public int HullPoints { get; set; }
	public FaceShieldPoints ShieldPoints { get; set; }

	public RunShip(string id, ShipConfiguration configuration, int hullPoints, FaceShieldPoints shieldPoints)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(shieldPoints);
		Id = id;
		Configuration = configuration;
		HullPoints = hullPoints;
		ShieldPoints = shieldPoints;
	}

	public static RunShip CreateDefault(string id, EType chassis)
	{
		var configuration = ShipCatalog.DefaultFor(chassis);
		return new RunShip(
			id,
			configuration,
			configuration.MaxHullPoints,
			configuration.Defenses.Clone());
	}

	public ShipSnapshot ToSnapshot() =>
		new(Id, Configuration.DeepCopy(), HullPoints, ShieldPoints.Clone());
}
