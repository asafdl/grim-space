using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Math;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.FleetSpawner;

public static class MapFleetFactory
{
	public static Fleet CreatePatrolFleet(
		FleetSpawnSpec spec,
		Coord coord,
		string fleetId,
		string memberIdentity)
	{
		var spawn = new Spawn(
			fleetId,
			spec.Type,
			coord,
			spec.PatrolRadius > 0 ? UnitDefaults.PatrolSpeedPerTick(spec.Type) : UnitDefaults.SpeedPerTick(spec.Type),
			UnitDefaults.EngageRadius(spec.Type),
			UnitDefaults.VisionRadius(spec.Type),
			[],
			spec.Faction,
			spec.PatrolRadius,
			spec.AggressionRatingOverride ?? AggressionRatingFor(spec, fleetId));
		var declarations = spec.Members.Select((member, index) => new ShipSpawnDeclaration(
			TypedIdGenerator.Format(UnitTypeSlug.For(member.Chassis), $"{memberIdentity}-{index}"),
			member.Chassis,
			member.GearTier,
			unchecked(spec.Seed + index))).ToArray();
		return Factory.Create(spawn, declarations);
	}

	private static int AggressionRatingFor(FleetSpawnSpec spec, string fleetId) =>
		spec.Type != EType.PirateFleet
			? 0
			: new StableRandom(StableSeedMixer.From(spec.Seed).Add(fleetId).Add("fleet-aggression").Value)
				.TriangularWeightedNumber(0, 10);
}
