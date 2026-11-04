using System.Text.Json;
using System.Text.Json.Serialization;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Run.Persistence;

/// <summary>
/// Timeline facts embed <see cref="BattleStatus"/>; delegates are not JSON-serializable.
/// Persist identity + payload shape, then rebuild callbacks via <see cref="BattleStatusRehydrator"/>.
/// </summary>
internal sealed class BattleStatusJsonConverter : JsonConverter<BattleStatus>
{
	public override BattleStatus Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		var dto = JsonSerializer.Deserialize<BattleStatusDto>(ref reader, options)
			?? throw new JsonException($"Unable to read '{nameof(BattleStatus)}'.");
		return BattleStatusRehydrator.Rehydrate(ShellFromDto(dto));
	}

	public override void Write(
		Utf8JsonWriter writer,
		BattleStatus value,
		JsonSerializerOptions options) =>
		JsonSerializer.Serialize(writer, ToDto(value), options);

	internal static BattleStatusDto ToDto(BattleStatus status)
	{
		ArgumentNullException.ThrowIfNull(status);
		var brace = status.Payload as ShieldBracePayload;
		var temporaryShield = status.Payload as TemporaryShieldPayload;
		var mark = status.Payload as MarkWeakspotPayload;
		return new BattleStatusDto(
			status.InstanceId,
			status.Priority,
			status.ActionTags.ToArray(),
			status.Polarity,
			brace?.Face ?? mark?.Face,
			brace is null ? null : CaptureFaces(brace.Snapshot),
			brace?.DamagePending ?? 0,
			temporaryShield is null ? null : CaptureFaces(temporaryShield.Remaining));
	}

	internal static BattleStatus ShellFromDto(BattleStatusDto dto)
	{
		ArgumentNullException.ThrowIfNull(dto);
		IBattleStatusPayload payload = (dto.Face, dto.Snapshot, dto.Remaining) switch
		{
			({ } face, { } snapshot, null) => new ShieldBracePayload
			{
				Face = face,
				Snapshot = RestoreFaces(snapshot),
				DamagePending = dto.DamagePending,
			},
			(null, null, { } remaining) => new TemporaryShieldPayload
			{
				Remaining = RestoreFaces(remaining),
			},
			({ } face, null, null) => new MarkWeakspotPayload { Face = face },
			(null, null, null) => EmptyStatusPayload.Instance,
			_ => throw new InvalidDataException(
				$"Status '{dto.InstanceId}' has inconsistent payload data."),
		};
		return new BattleStatus
		{
			InstanceId = dto.InstanceId,
			Priority = dto.Priority,
			ActionTags = dto.ActionTags,
			Polarity = dto.Polarity,
			Payload = payload,
		};
	}

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
