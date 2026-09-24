using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.World.StarSystem.Units;

public static class Factory
{
	public static ContractFleetSpawns Create(
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
			.Select((spawn, index) => Create(
				new Spawn(
					spawn.UnitId,
					spawn.Spawn.Type,
					"",
					spawn.Coord,
					UnitDefaults.SpeedPerTick(spawn.Spawn.Type),
					UnitDefaults.EngageRadius(spawn.Spawn.Type),
					UnitDefaults.VisionRadius(spawn.Spawn.Type),
					[],
					spawn.Spawn.Faction,
					new CombatProfile(spawn.Spawn.Danger, spawn.Spawn.Seed)),
				spawn.Spawn.MemberTypes,
				$"{memberIdentity}-{index}"))
			.ToArray();

		return new ContractFleetSpawns(fleets);
	}

	public static Fleet CreateAmbushFleet(
		FleetSpawnSpec spec,
		Coord coord,
		string unitId,
		string memberIdentity)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentException.ThrowIfNullOrEmpty(unitId);
		ArgumentException.ThrowIfNullOrEmpty(memberIdentity);

		return Create(
			new Spawn(
				unitId,
				spec.Type,
				"",
				coord,
				UnitDefaults.SpeedPerTick(spec.Type),
				UnitDefaults.EngageRadius(spec.Type),
				UnitDefaults.VisionRadius(spec.Type),
				[],
				spec.Faction,
				new CombatProfile(spec.Danger, spec.Seed)),
			spec.MemberTypes,
			memberIdentity);
	}

	public static Fleet Create(Spawn spawn) => Create(spawn, Array.Empty<ShipSpawnDeclaration>());

	public static Fleet Create(Spawn spawn, IReadOnlyList<BattleUnitType> memberTypes) =>
		Create(spawn, DeclarationsFor(memberTypes, TypedIdGenerator.NextInstanceSlug()));

	public static Fleet Create(
		Spawn spawn,
		IReadOnlyList<BattleUnitType> memberTypes,
		string memberIdentity)
	{
		ArgumentException.ThrowIfNullOrEmpty(memberIdentity);
		return Create(spawn, DeclarationsFor(memberTypes, memberIdentity));
	}

	public static Fleet Create(Spawn spawn, IReadOnlyList<ShipSpawnDeclaration> declarations)
	{
		ArgumentNullException.ThrowIfNull(spawn);
		ArgumentNullException.ThrowIfNull(declarations);
		ArgumentException.ThrowIfNullOrEmpty(spawn.Id);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(spawn.SpeedPerTick, 0);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(spawn.EngageRadius, 0);
		if (!double.IsFinite(spawn.VisionRadius) || spawn.VisionRadius <= 0)
			throw new ArgumentOutOfRangeException(nameof(spawn), "VisionRadius must be finite and greater than zero.");
		ArgumentNullException.ThrowIfNull(spawn.ChoreDockIds);

		var members = declarations.Select(declaration => new FleetMember(declaration.ShipId)).ToArray();
		return new Fleet(State.FromSpawn(spawn), members, declarations);
	}

	private static IReadOnlyList<ShipSpawnDeclaration> DeclarationsFor(
		IReadOnlyList<BattleUnitType> memberTypes,
		string memberIdentity) =>
		memberTypes
			.Select((type, index) => new ShipSpawnDeclaration(
				TypedIdGenerator.Format(UnitTypeSlug.For(type), $"{memberIdentity}-{index}"),
				type))
			.ToArray();
}

public sealed record ContractFleetSpawns(IReadOnlyList<Fleet> Fleets)
{
	public static ContractFleetSpawns Empty { get; } = new([]);
}
