using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.World.StarSystem.Units;

public static class Factory
{
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
