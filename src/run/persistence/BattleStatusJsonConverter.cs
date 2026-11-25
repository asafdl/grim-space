using System.Text.Json;
using System.Text.Json.Serialization;
using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Run.Persistence;

internal sealed class BattleStatusJsonConverter : JsonConverter<BattleStatus>
{
	public override BattleStatus Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		var dto = JsonSerializer.Deserialize<BattleStatusDto>(ref reader, options)
			?? throw new JsonException($"Unable to read '{nameof(BattleStatus)}'.");
		return FromDto(dto);
	}

	public override void Write(
		Utf8JsonWriter writer,
		BattleStatus value,
		JsonSerializerOptions options) =>
		JsonSerializer.Serialize(writer, ToDto(value), options);

	internal static BattleStatusDto ToDto(BattleStatus status)
	{
		ArgumentNullException.ThrowIfNull(status);
		return status switch
		{
			ShieldBraceStatus brace => new(
				nameof(ShieldBraceStatus),
				brace.Polarity,
				brace.Face,
				CaptureFaces(brace.Snapshot),
				brace.DamagePending),
			TemporaryShieldGrantStatus grant => new(
				nameof(TemporaryShieldGrantStatus),
				grant.Polarity,
				Remaining: CaptureFaces(grant.Remaining),
				GrantId: grant.GrantId),
			MarkWeakspotDebuff mark => new(
				nameof(MarkWeakspotDebuff),
				mark.Polarity,
				mark.Face),
			ThirstForScrap thirst => new(nameof(ThirstForScrap), thirst.Polarity, Stacks: thirst.Stacks),
			ReactiveShields reactive => new(nameof(ReactiveShields), reactive.Polarity, Stacks: reactive.Stacks),
			ProtectionDroneBuff => new(nameof(ProtectionDroneBuff), status.Polarity),
			SpawnedAllyDamageBuff => new(nameof(SpawnedAllyDamageBuff), status.Polarity),
			ScrapDroneSwarmDamageBonus => new(nameof(ScrapDroneSwarmDamageBonus), status.Polarity),
			HealingSkillRangeBonus => new(nameof(HealingSkillRangeBonus), status.Polarity),
			LightningShieldDamageBonus => new(nameof(LightningShieldDamageBonus), status.Polarity),
			AdjacentDamageBonus => new(nameof(AdjacentDamageBonus), status.Polarity),
			ReinforcedHull => new(nameof(ReinforcedHull), status.Polarity),
			InTheFray => new(nameof(InTheFray), status.Polarity),
			StationaryDamageDeflection => new(nameof(StationaryDamageDeflection), status.Polarity),
			ShieldDynamo => new(nameof(ShieldDynamo), status.Polarity),
			YouLittleRat => new(nameof(YouLittleRat), status.Polarity),
			NothingButMetal => new(nameof(NothingButMetal), status.Polarity),
			SpawnedUnitDamageBonus => new(nameof(SpawnedUnitDamageBonus), status.Polarity),
			OneWithTheVoid => new(nameof(OneWithTheVoid), status.Polarity),
			OutgoingDamageBonus => new(nameof(OutgoingDamageBonus), status.Polarity),
			IncreasedHullDamageTaken => new(nameof(IncreasedHullDamageTaken), status.Polarity),
			SpawnedAllyDamageBonus => new(nameof(SpawnedAllyDamageBonus), status.Polarity),
			DamageResistance => new(nameof(DamageResistance), status.Polarity),
			PirateDamageBonus => new(nameof(PirateDamageBonus), status.Polarity),
			HealBonus => new(nameof(HealBonus), status.Polarity),
			HealPenalty => new(nameof(HealPenalty), status.Polarity),
			_ => throw new NotSupportedException(
				$"Battle status '{status.GetType().Name}' cannot be persisted."),
		};
	}

	internal static BattleStatus FromDto(BattleStatusDto dto)
	{
		ArgumentNullException.ThrowIfNull(dto);
		return dto.TypeName switch
		{
			nameof(ShieldBraceStatus) => new ShieldBraceStatus
			{
				Face = dto.Face ?? throw Missing(dto.TypeName, nameof(dto.Face)),
				Snapshot = RestoreFaces(dto.Snapshot ?? throw Missing(dto.TypeName, nameof(dto.Snapshot))),
				DamagePending = dto.DamagePending,
			},
			nameof(TemporaryShieldGrantStatus) => new TemporaryShieldGrantStatus
			{
				GrantId = dto.GrantId ?? throw Missing(dto.TypeName, nameof(dto.GrantId)),
				Remaining = RestoreFaces(dto.Remaining ?? throw Missing(dto.TypeName, nameof(dto.Remaining))),
			},
			nameof(MarkWeakspotDebuff) => new MarkWeakspotDebuff
			{
				Face = dto.Face ?? throw Missing(dto.TypeName, nameof(dto.Face)),
			},
			nameof(ThirstForScrap) => new ThirstForScrap { Stacks = dto.Stacks },
			nameof(ReactiveShields) => new ReactiveShields { Stacks = dto.Stacks },
			nameof(ProtectionDroneBuff) => new ProtectionDroneBuff(),
			nameof(SpawnedAllyDamageBuff) => new SpawnedAllyDamageBuff(),
			nameof(ScrapDroneSwarmDamageBonus) => new ScrapDroneSwarmDamageBonus(),
			nameof(HealingSkillRangeBonus) => new HealingSkillRangeBonus(),
			nameof(LightningShieldDamageBonus) => new LightningShieldDamageBonus(),
			nameof(AdjacentDamageBonus) => new AdjacentDamageBonus(),
			nameof(ReinforcedHull) => new ReinforcedHull(),
			nameof(InTheFray) => new InTheFray(),
			nameof(StationaryDamageDeflection) => new StationaryDamageDeflection(),
			nameof(ShieldDynamo) => new ShieldDynamo(),
			nameof(YouLittleRat) => new YouLittleRat(),
			nameof(NothingButMetal) => new NothingButMetal(),
			nameof(SpawnedUnitDamageBonus) => new SpawnedUnitDamageBonus(),
			nameof(OneWithTheVoid) => new OneWithTheVoid(),
			nameof(OutgoingDamageBonus) => new OutgoingDamageBonus(),
			nameof(IncreasedHullDamageTaken) => new IncreasedHullDamageTaken(),
			nameof(SpawnedAllyDamageBonus) => new SpawnedAllyDamageBonus(),
			nameof(DamageResistance) => new DamageResistance(),
			nameof(PirateDamageBonus) => new PirateDamageBonus(),
			nameof(HealBonus) => new HealBonus(),
			nameof(HealPenalty) => new HealPenalty(),
			_ => throw new NotSupportedException($"Unknown battle status type '{dto.TypeName}'."),
		};
	}

	private static InvalidDataException Missing(string typeName, string field) =>
		new($"Status '{typeName}' is missing '{field}'.");

	private static FaceShieldDto CaptureFaces(FaceShieldPoints points) =>
		new(Enum.GetValues<ESpatialOrientation>().Select(face => points[face]).ToArray());

	private static FaceShieldPoints RestoreFaces(FaceShieldDto dto)
	{
		var faces = Enum.GetValues<ESpatialOrientation>();
		if (dto.Points.Length != faces.Length)
			throw new InvalidDataException("Shield face data has an invalid length.");
		var points = new FaceShieldPoints();
		for (var index = 0; index < faces.Length; index++)
			points[faces[index]] = dto.Points[index];
		return points;
	}
}
