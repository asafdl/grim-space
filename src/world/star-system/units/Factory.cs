using GrimSpace.Core.Ids;
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
			world.Seed,
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
		var bindings = planned
			.GroupBy(spawn => spawn.GroupId, StringComparer.Ordinal)
			.ToDictionary(
				group => group.Key,
				group => (IReadOnlyList<string>)group.Select(spawn => spawn.UnitId).ToArray(),
				StringComparer.Ordinal);

		return new ContractFleetSpawns(fleets, bindings);
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

public sealed record ContractFleetSpawns(
	IReadOnlyList<Fleet> Fleets,
	IReadOnlyDictionary<string, IReadOnlyList<string>> Bindings)
{
	public static ContractFleetSpawns Empty { get; } =
		new([], ContractState.EmptyBindings);
}
