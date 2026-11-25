using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

public static class TemporaryShields
{
	public static string NewGrantId() => Guid.NewGuid().ToString("N");

	public static int TotalOnFace(State unit, ESpatialOrientation face)
	{
		var total = 0;
		foreach (var status in unit.Statuses)
		{
			if (status is TemporaryShieldGrantStatus grant)
				total += grant.Remaining[face];
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
			var max = unit.Loadout.BaseShieldPoints[face];
			remaining[face] = (int)System.Math.Round(
				max * percentOfFaceMax / 100m,
				System.MidpointRounding.AwayFromZero);
		}

		AddGrant(unit, grantId, remaining);
	}

	public static IReadOnlyList<TemporaryShieldGrant> ExtractGrants(State unit)
	{
		ArgumentNullException.ThrowIfNull(unit);
		return unit.Statuses
			.OfType<TemporaryShieldGrantStatus>()
			.Select(status => new TemporaryShieldGrant
			{
				GrantId = status.GrantId,
				Remaining = status.Remaining.Clone(),
			})
			.ToArray();
	}

	public static void ReplaceGrants(State unit, IEnumerable<TemporaryShieldGrant> grants)
	{
		ArgumentNullException.ThrowIfNull(unit);
		ArgumentNullException.ThrowIfNull(grants);
		unit.RemoveStatus<TemporaryShieldGrantStatus>();
		foreach (var grant in grants)
			AddGrant(unit, grant.GrantId, grant.Remaining.Clone());
	}

	public static bool TryRevokeGrant(State unit, string grantId, out TemporaryShieldGrant? removed)
	{
		var status = unit.Statuses.OfType<TemporaryShieldGrantStatus>()
			.FirstOrDefault(grant => grant.GrantId == grantId);
		removed = status is null
			? null
			: new TemporaryShieldGrant
			{
				GrantId = status.GrantId,
				Remaining = status.Remaining.Clone(),
			};
		if (status is null)
			return false;

		unit.Statuses.Remove(status);
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
		foreach (var status in unit.Statuses.OfType<TemporaryShieldGrantStatus>().ToArray())
		{
			if (remaining <= 0)
				break;

			var onGrant = status.Remaining[face];
			var absorbed = System.Math.Min(onGrant, remaining);
			status.Remaining[face] = onGrant - absorbed;
			remaining -= absorbed;
		}

		unit.Statuses.RemoveAll(status =>
			status is TemporaryShieldGrantStatus { Remaining.MaxOnAnyFace: 0 });
		return amount - remaining;
	}

	private static void AddGrant(State unit, string grantId, FaceShieldPoints remaining) =>
		unit.Statuses.Add(
			new TemporaryShieldGrantStatus
			{
				GrantId = grantId,
				Remaining = remaining,
			});
}
