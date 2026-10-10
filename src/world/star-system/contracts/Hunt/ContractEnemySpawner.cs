using GrimSpace.Core.Ids;
using GrimSpace.Math;
using GrimSpace.Units.Enums;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractEnemySpawner
{
	public static ContractFleetSpawns SpawnForContract(
		Contract contract,
		StarMap world,
		string memberIdentity)
	{
		ArgumentNullException.ThrowIfNull(contract);
		ArgumentNullException.ThrowIfNull(world);
		ArgumentException.ThrowIfNullOrEmpty(memberIdentity);

		if (contract.Objective is not IHasSpawnGroups hasSpawnGroups)
			return ContractFleetSpawns.Empty;

		var planned = ContractFleetPlacement.Plan(
			hasSpawnGroups.SpawnGroups,
			contract.Id,
			world,
			world.FleetRegistry);
		var fleets = planned
			.Select((spawn, index) => CreateFleet(
				spawn.UnitId,
				spawn.Coord,
				spawn.Spawn,
				$"{memberIdentity}-{index}",
				contract.Id))
			.ToArray();

		return new ContractFleetSpawns(fleets);
	}

	public static Fleet CreateAmbushFleet(
		FleetSpawnSpec spec,
		Coord coord,
		string unitId,
		string memberIdentity,
		string? sourceContractId = null) =>
		CreateFleet(unitId, coord, spec, memberIdentity, sourceContractId);

	private static Fleet CreateFleet(
		string unitId,
		Coord coord,
		FleetSpawnSpec spec,
		string memberIdentity,
		string? sourceContractId)
	{
		var spawn = new Spawn(
			unitId,
			spec.Type,
			coord,
			spec.PatrolRadius > 0
				? UnitDefaults.PatrolSpeedPerTick(spec.Type)
				: UnitDefaults.SpeedPerTick(spec.Type),
			UnitDefaults.EngageRadius(spec.Type),
			UnitDefaults.VisionRadius(spec.Type),
			[],
			spec.Faction,
			spec.PatrolRadius,
			AggressionRatingFor(spec, unitId),
			sourceContractId);
		var declarations = DeclarationsFor(spec, memberIdentity);
		return Factory.Create(spawn, declarations);
	}

	private static int AggressionRatingFor(FleetSpawnSpec spec, string unitId) =>
		spec.Type != FleetType.PirateFleet
			? 0
			: spec.AggressionRatingOverride
				?? new StableRandom(
					StableSeedMixer.From(spec.Seed)
						.Add(unitId)
						.Add("fleet-aggression")
						.Value)
					.TriangularWeightedNumber(0, 10);

	private static IReadOnlyList<ShipSpawnDeclaration> DeclarationsFor(
		FleetSpawnSpec spec,
		string memberIdentity) =>
		spec.Members
			.Select((ship, index) => new ShipSpawnDeclaration(
				TypedIdGenerator.Format(
					UnitTypeSlug.For(ship.Chassis),
					$"{memberIdentity}-{index}"),
				ship.Chassis,
				ship.GearTier,
				unchecked(spec.Seed + index)))
			.ToArray();
}
