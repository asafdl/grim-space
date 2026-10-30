using GrimSpace.Run;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

internal sealed record FleetPilotView(
	string Id,
	string Name,
	string PortraitId,
	EOperatorClassId ClassId,
	string? AssignedShipId,
	bool IsActive);

internal sealed record FleetAbilityStatView(
	string Label,
	string Value);

internal sealed record FleetAbilityView(
	EAbilityKind Kind,
	string Name,
	IReadOnlyList<FleetAbilityStatView> Stats,
	string Facets);

internal sealed record FleetShipView(
	string Id,
	string Chassis,
	EOperatorClassId ClassId,
	EShipGearTier GearTier,
	int Hull,
	int MaxHull,
	int Shields,
	int MaxShields,
	int ActionPoints,
	int ManeuverPoints,
	int Initiative,
	FleetPilotView? Pilot,
	IReadOnlyList<FleetAbilityView> Abilities);

internal sealed record FleetManagementView(
	IReadOnlyList<FleetShipView> Ships,
	IReadOnlyList<FleetPilotView> Pilots);

internal static class FleetManagementProjection
{
	public static FleetManagementView Collect(State run)
	{
		ArgumentNullException.ThrowIfNull(run);
		var pilots = run.PilotRegistry.All
			.OrderByDescending(pilot => pilot.IsActive)
			.ThenBy(pilot => pilot.Name, StringComparer.Ordinal)
			.Select(pilot => new FleetPilotView(
				pilot.Id,
				pilot.Name,
				pilot.PortraitId,
				pilot.ClassId,
				pilot.AssignedShipId,
				pilot.IsActive))
			.ToArray();
		var pilotsByShipId = pilots
			.Where(pilot => pilot.AssignedShipId is not null)
			.ToDictionary(pilot => pilot.AssignedShipId!, StringComparer.Ordinal);
		var ships = run.ItemInventory
			.ShipsOwnedBy(State.PlayerFleetUnitId)
			.OrderBy(ship => ship.Spec.Chassis)
			.ThenBy(ship => ship.Id, StringComparer.Ordinal)
			.Select(ship => new FleetShipView(
				ship.Id,
				ChassisName(ship.Spec.Chassis),
				ship.Spec.ClassId,
				ShipLoadoutGearTier.FromLoadout(ship.Loadout, ship.Spec.Chassis),
				ship.HullPoints,
				ship.Loadout.MaxHullPoints,
				ship.TotalCurrentShieldPoints,
				ship.TotalMaxShieldPoints,
				ship.Spec.Maneuverability.MaxActionPoints,
				ship.Spec.Maneuverability.MaxManeuverPoints,
				ship.Spec.Initiative,
				pilotsByShipId.GetValueOrDefault(ship.Id),
				ship.Loadout.InstalledAbilities
					.GroupBy(installed => new
					{
						installed.Kind,
						Name = AbilityName(installed.Kind),
						Stats = AbilityStats(installed.Spec),
					})
					.Select(group => new FleetAbilityView(
						group.Key.Kind,
						group.Key.Name,
						group.Key.Stats,
						string.Join(
							" · ",
							group
								.Select(installed => FacetName(installed.MountedOn))
								.Distinct(StringComparer.Ordinal)
								.Select(facet => facet.ToUpperInvariant()))))
					.ToArray()))
			.ToArray();
		return new FleetManagementView(ships, pilots);
	}

	private static string ChassisName(EType chassis) =>
		chassis switch
		{
			EType.IndustrialGooper => "Industrial Gooper",
			EType.RepurposedMiner => "Repurposed Miner",
			EType.VoidBomb => "Void Bomb",
			_ => chassis.ToString(),
		};

	private static string FacetName(ESpatialOrientation facet) =>
		facet switch
		{
			ESpatialOrientation.Forward => "Forward",
			ESpatialOrientation.Retro => "Aft",
			ESpatialOrientation.Starboard => "Starboard",
			ESpatialOrientation.Port => "Port",
			ESpatialOrientation.Dorsal => "Dorsal",
			ESpatialOrientation.Ventral => "Ventral",
			_ => facet.ToString(),
		};

	private static string AbilityName(EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.ScrapDroneSwarm => "Scrap Drone Swarm",
			EAbilityKind.LightningCannon => "Lightning Cannon",
			EAbilityKind.MinerBay => "Miner Bay",
			EAbilityKind.VoidBombLauncher => "Void Bomb Launcher",
			EAbilityKind.GoopGun => "Goop Gun",
			_ => kind.ToString(),
		};

	private static IReadOnlyList<FleetAbilityStatView> AbilityStats(AbilitySpec spec)
		=> spec switch
		{
			ScrapDroneSwarmSpec swarm =>
			[
				new("DAMAGE", swarm.Damage.ToString()),
				new("ALLY HEAL", swarm.Heal.ToString()),
				new("RANGE", AbilityReach.MaxManhattanFromFirer(swarm).ToString()),
				new("USES PER TURN", swarm.UsesPerTurn.ToString()),
			],
			LightningCannonSpec cannon =>
			[
				new("DAMAGE", cannon.Damage.ToString()),
				new("RANGE", AbilityReach.MaxManhattanFromFirer(cannon).ToString()),
				new("USES PER TURN", cannon.UsesPerTurn.ToString()),
			],
			MinerBaySpec bay =>
			[
				new("ACTIVE MINER CAP", bay.MaxLivingChildren.ToString()),
				new("COOLDOWN", bay.CooldownTurns.ToString()),
			],
			VoidBombLauncherSpec launcher =>
			[
				new("BLAST DAMAGE", launcher.BlastDamage.ToString()),
				new("BLAST RADIUS", launcher.BlastRadius.ToString()),
				new("FUEL TURNS", launcher.FuelTurns.ToString()),
				new("COOLDOWN", launcher.CooldownTurns.ToString()),
			],
			GoopGunSpec goop =>
			[
				new("RANGE", goop.Range.ToString()),
				new("AREA", $"{goop.HalfWidth * 2 + 1} × {goop.HalfHeight * 2 + 1}"),
				new("COOLDOWN", goop.UnavailableTurns.ToString()),
			],
			_ => [],
		};
}
