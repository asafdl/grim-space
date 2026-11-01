using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

public sealed class TemporaryShieldPayload : IBattleStatusPayload
{
	public required FaceShieldPoints Remaining { get; init; }

	public IBattleStatusPayload DeepCopy() =>
		new TemporaryShieldPayload { Remaining = Remaining.Clone() };
}

public static class TemporaryShields
{
	private const string GrantKeyPrefix = "TemporaryShield:";

	public static string NewGrantId() => Guid.NewGuid().ToString("N");

	public static int TotalOnFace(State unit, ESpatialOrientation face)
	{
		var total = 0;
		foreach (var status in unit.Statuses)
		{
			if (status.Payload is TemporaryShieldPayload payload)
				total += payload.Remaining[face];
		}

		return total;
	}

	public static void GrantPerFace(State unit, string grantId, int perFace)
	{
		var remaining = new FaceShieldPoints();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			remaining[face] = perFace;
		AddGrant(unit, grantId, remaining);
	}

	public static void GrantPerFacePercentOfMax(State unit, string grantId, int percentOfFaceMax)
	{
		var remaining = new FaceShieldPoints();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var max = unit.Loadout.MaxShieldPoints[face];
			remaining[face] = (int)System.Math.Round(
				max * percentOfFaceMax / 100m,
				System.MidpointRounding.AwayFromZero);
		}

		AddGrant(unit, grantId, remaining);
	}

	public static IReadOnlyList<TemporaryShieldGrant> ExtractGrants(State unit)
	{
		ArgumentNullException.ThrowIfNull(unit);
		var prefixLength = $"buff:{GrantKeyPrefix}".Length;
		var suffixLength = unit.Id.Length + 1;
		return unit.Statuses
			.Where(status => status.Payload is TemporaryShieldPayload)
			.Select(status => new TemporaryShieldGrant
			{
				GrantId = status.InstanceId[prefixLength..^suffixLength],
				Remaining = ((TemporaryShieldPayload)status.Payload).Remaining.Clone(),
			})
			.ToArray();
	}

	public static void ReplaceGrants(State unit, IEnumerable<TemporaryShieldGrant> grants)
	{
		ArgumentNullException.ThrowIfNull(unit);
		ArgumentNullException.ThrowIfNull(grants);
		unit.Statuses.RemoveAll(status => status.Payload is TemporaryShieldPayload);
		foreach (var grant in grants)
			AddGrant(unit, grant.GrantId, grant.Remaining.Clone());
	}

	public static bool TryRevokeGrant(State unit, string grantId, out TemporaryShieldGrant? removed)
	{
		var grants = ExtractGrants(unit);
		removed = grants.FirstOrDefault(grant => grant.GrantId == grantId);
		if (removed is null)
			return false;

		BattleStatuses.RemoveInstance(
			unit.Statuses,
			BattleStatuses.BuffInstanceId($"{GrantKeyPrefix}{grantId}", unit.Id));
		return true;
	}

	public static void InsertGrant(State unit, int index, TemporaryShieldGrant grant)
	{
		ArgumentNullException.ThrowIfNull(unit);
		ArgumentNullException.ThrowIfNull(grant);
		var grants = ExtractGrants(unit).ToList();
		grants.Insert(System.Math.Clamp(index, 0, grants.Count), grant.Clone());
		ReplaceGrants(unit, grants);
	}

	public static int AbsorbOnFace(State unit, ESpatialOrientation face, int amount)
	{
		if (amount <= 0)
			return 0;

		var remaining = amount;
		foreach (var status in unit.Statuses)
		{
			if (remaining <= 0)
				break;

			if (status.Payload is not TemporaryShieldPayload payload)
				continue;

			var onGrant = payload.Remaining[face];
			var absorbed = System.Math.Min(onGrant, remaining);
			payload.Remaining[face] = onGrant - absorbed;
			remaining -= absorbed;
		}

		unit.Statuses.RemoveAll(status =>
			status.Payload is TemporaryShieldPayload { Remaining.MaxOnAnyFace: 0 });
		return amount - remaining;
	}

	private static void AddGrant(State unit, string grantId, FaceShieldPoints remaining) =>
		unit.Statuses.Add(
			new BattleStatus
			{
				InstanceId = BattleStatuses.BuffInstanceId($"{GrantKeyPrefix}{grantId}", unit.Id),
				Priority = 50,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				Payload = new TemporaryShieldPayload { Remaining = remaining },
			});
}
