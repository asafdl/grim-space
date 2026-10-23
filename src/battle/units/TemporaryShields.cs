using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Units;

public static class TemporaryShields
{
	public static string NewGrantId() => Guid.NewGuid().ToString("N");

	public static int TotalOnFace(State unit, ESpatialOrientation face)
	{
		var total = 0;
		foreach (var grant in unit.TemporaryShieldGrants)
			total += grant.Remaining[face];
		return total;
	}

	public static void GrantPerFace(State unit, string grantId, int perFace)
	{
		var grant = new TemporaryShieldGrant { GrantId = grantId };
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			grant.Remaining[face] = perFace;
		unit.TemporaryShieldGrants.Add(grant);
	}

	public static void GrantPerFacePercentOfMax(State unit, string grantId, int percentOfFaceMax)
	{
		var grant = new TemporaryShieldGrant { GrantId = grantId };
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var max = unit.Loadout.MaxShieldPoints[face];
			grant.Remaining[face] = (int)System.Math.Round(
				max * percentOfFaceMax / 100m,
				System.MidpointRounding.AwayFromZero);
		}

		unit.TemporaryShieldGrants.Add(grant);
	}

	public static bool TryRevokeGrant(State unit, string grantId, out TemporaryShieldGrant? removed)
	{
		var index = unit.TemporaryShieldGrants.FindIndex(grant => grant.GrantId == grantId);
		if (index < 0)
		{
			removed = null;
			return false;
		}

		removed = unit.TemporaryShieldGrants[index];
		unit.TemporaryShieldGrants.RemoveAt(index);
		return true;
	}

	public static int AbsorbOnFace(State unit, ESpatialOrientation face, int amount)
	{
		if (amount <= 0)
			return 0;

		var remaining = amount;
		foreach (var grant in unit.TemporaryShieldGrants)
		{
			if (remaining <= 0)
				break;

			var onGrant = grant.Remaining[face];
			var absorbed = System.Math.Min(onGrant, remaining);
			grant.Remaining[face] = onGrant - absorbed;
			remaining -= absorbed;
		}

		PruneDepleted(unit);
		return amount - remaining;
	}

	private static void PruneDepleted(State unit)
	{
		for (var i = unit.TemporaryShieldGrants.Count - 1; i >= 0; i--)
		{
			if (unit.TemporaryShieldGrants[i].Remaining.MaxOnAnyFace == 0)
				unit.TemporaryShieldGrants.RemoveAt(i);
		}
	}
}
