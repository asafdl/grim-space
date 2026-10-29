using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

public static class ShieldBrace
{
	public static bool IsActive(State unit) => unit.ShieldBraceActive;

	public static void Activate(State unit, ESpatialOrientation targetFace)
	{
		ArgumentNullException.ThrowIfNull(unit);
		unit.ShieldBraceSnapshot = unit.ShieldPoints.Clone();
		var percent = PilotSkillPercents.Brace.ShieldConsolidationPercent;
		var pooled = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var current = unit.ShieldPoints[face];
			var moved = ScalePercent(current, percent);
			unit.ShieldPoints[face] = current - moved;
			pooled += moved;
		}

		unit.ShieldPoints[targetFace] += pooled;
		unit.ShieldBraceActive = true;
		unit.ShieldBraceFace = targetFace;
		unit.ShieldBraceDamagePending = 0;
		unit.ShieldBraceUsedThisTurn = true;
	}

	public static void RecordShieldDamage(State unit, ESpatialOrientation face, int absorbed)
	{
		if (!unit.ShieldBraceActive
			|| unit.ShieldBraceFace != face
			|| absorbed <= 0)
			return;

		unit.ShieldBraceDamagePending += absorbed;
	}

	public static void Release(State unit)
	{
		if (!unit.ShieldBraceActive)
			return;

		var bracedFace = unit.ShieldBraceFace!.Value;
		var snapshot = unit.ShieldBraceSnapshot!;
		var pending = unit.ShieldBraceDamagePending;
		if (pending > 0)
			ApplyPendingDamageToOtherFaces(unit, bracedFace, pending);

		RestoreProportionalShields(unit, snapshot);
		Clear(unit);
	}

	public static void Clear(State unit)
	{
		unit.ShieldBraceActive = false;
		unit.ShieldBraceFace = null;
		unit.ShieldBraceSnapshot = null;
		unit.ShieldBraceDamagePending = 0;
	}

	public static int TotalPermanentShields(State unit)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += unit.ShieldPoints[face];
		return total;
	}

	private static int TotalPermanentShields(FaceShieldPoints shields)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += shields[face];
		return total;
	}

	private static void ApplyPendingDamageToOtherFaces(
		State unit,
		ESpatialOrientation bracedFace,
		int shieldDamage)
	{
		var others = Enum.GetValues<ESpatialOrientation>()
			.Where(face => face != bracedFace)
			.ToArray();
		if (others.Length == 0)
			return;

		var remaining = shieldDamage;
		for (var i = 0; i < others.Length && remaining > 0; i++)
		{
			var share = shieldDamage / others.Length
				+ (i < shieldDamage % others.Length ? 1 : 0);
			var face = others[i];
			var absorbed = System.Math.Min(unit.ShieldPoints[face], share);
			unit.ShieldPoints[face] -= absorbed;
			remaining -= share;
		}
	}

	private static void RestoreProportionalShields(State unit, FaceShieldPoints snapshot)
	{
		var total = TotalPermanentShields(unit);
		var snapshotTotal = TotalPermanentShields(snapshot);
		if (total <= 0 || snapshotTotal <= 0)
		{
			unit.ShieldPoints.Fill(0);
			return;
		}

		var faces = Enum.GetValues<ESpatialOrientation>();
		var max = unit.Loadout.MaxShieldPoints;
		var assigned = 0;
		for (var i = 0; i < faces.Length; i++)
		{
			var face = faces[i];
			var target = i == faces.Length - 1
				? total - assigned
				: (int)System.Math.Round(
					total * (decimal)snapshot[face] / snapshotTotal,
					System.MidpointRounding.AwayFromZero);
			target = System.Math.Min(target, max[face]);
			unit.ShieldPoints[face] = target;
			assigned += target;
		}

		var leftover = total - assigned;
		while (leftover > 0)
		{
			var progress = false;
			foreach (var face in faces)
			{
				var room = max[face] - unit.ShieldPoints[face];
				if (room <= 0)
					continue;

				var add = System.Math.Min(room, leftover);
				unit.ShieldPoints[face] += add;
				leftover -= add;
				progress = true;
				if (leftover <= 0)
					break;
			}

			if (!progress)
				break;
		}
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);
}
