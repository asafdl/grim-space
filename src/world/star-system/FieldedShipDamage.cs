using GrimSpace.Math.Grid;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem;

/// <summary>
/// Hull or shield change for fielded ships. <see cref="Apply"/> spends shields evenly across faces, then hull.
/// The run applies <see cref="FieldedShipChange.After"/> only while the ship still matches <see cref="FieldedShipChange.Before"/>.
/// </summary>
public sealed record FieldedShipDamage(
	string Cause,
	IReadOnlyList<FieldedShipChange> Ships)
{
	public static ShipInstance Apply(ShipInstance before, int amount, int minimumHull = 0)
	{
		ArgumentNullException.ThrowIfNull(before);
		ArgumentOutOfRangeException.ThrowIfNegative(amount);
		ArgumentOutOfRangeException.ThrowIfNegative(minimumHull);

		var shields = before.CurrentShieldPoints.Clone();
		var remaining = amount;
		while (remaining > 0)
		{
			var absorbed = false;
			foreach (var face in Enum.GetValues<ESpatialOrientation>())
			{
				if (remaining <= 0)
					break;
				if (shields[face] <= 0)
					continue;

				shields[face]--;
				remaining--;
				absorbed = true;
			}

			if (!absorbed)
				break;
		}

		var hull = before.CurrentHullPoints - remaining;
		if (hull < minimumHull)
			hull = System.Math.Min(before.CurrentHullPoints, minimumHull);

		return new ShipInstance(before.Id, before.Spec, before.Loadout, hull, shields);
	}
}

public sealed record FieldedShipChange(
	string ShipId,
	ShipInstance Before,
	ShipInstance After);
