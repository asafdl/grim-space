using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

public static class ShieldBrace
{
	public static bool IsActive(State unit) => TryStatus(unit) is not null;

	public static ShieldBraceStatus CreateStatus(
		string actorId,
		ESpatialOrientation face,
		FaceShieldPoints snapshot,
		int damagePending) =>
		new()
		{
			Face = face,
			Snapshot = snapshot.Clone(),
			DamagePending = damagePending,
		};

	public static ShieldBraceStatus? TryStatus(State unit) => unit.StatusOf<ShieldBraceStatus>();

	public static void Restore(State unit, ShieldBraceStatus? previous)
	{
		Clear(unit);
		if (previous is not null)
			unit.Statuses.Add(previous);
	}

	public static void Activate(State unit, ESpatialOrientation targetFace)
	{
		ArgumentNullException.ThrowIfNull(unit);
		var snapshot = unit.CurrentShieldPoints.Clone();
		var percent = PilotSkillPercents.Brace.ShieldConsolidationPercent;
		var pooled = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var current = unit.CurrentShieldPoints[face];
			var moved = ScalePercent(current, percent);
			unit.CurrentShieldPoints[face] = current - moved;
			pooled += moved;
		}

		unit.CurrentShieldPoints[targetFace] += pooled;
		unit.ShieldBraceUsedThisTurn = true;
		Clear(unit);
		unit.Statuses.Add(CreateStatus(unit.Id, targetFace, snapshot, damagePending: 0));
	}

	public static void RecordShieldDamage(State unit, ESpatialOrientation face, int absorbed)
	{
		var status = TryStatus(unit);
		if (status is null || status.Face != face || absorbed <= 0)
			return;

		status.DamagePending += absorbed;
	}

	public static void Release(State unit)
	{
		var status = TryStatus(unit);
		if (status is null)
			return;

		if (status.DamagePending > 0)
			ApplyPendingDamageToOtherFaces(unit, status.Face, status.DamagePending);

		RestoreProportionalShields(unit, status.Snapshot);
		Clear(unit);
	}

	public static void Clear(State unit) => unit.RemoveStatus<ShieldBraceStatus>();

	public static int TotalPermanentShields(State unit)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += unit.CurrentShieldPoints[face];
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
			var absorbed = System.Math.Min(unit.CurrentShieldPoints[face], share);
			unit.CurrentShieldPoints[face] -= absorbed;
			remaining -= share;
		}
	}

	private static void RestoreProportionalShields(State unit, FaceShieldPoints snapshot)
	{
		var total = TotalPermanentShields(unit);
		var snapshotTotal = TotalPermanentShields(snapshot);
		if (total <= 0 || snapshotTotal <= 0)
		{
			unit.CurrentShieldPoints.Fill(0);
			return;
		}

		var faces = Enum.GetValues<ESpatialOrientation>();
		var max = unit.Loadout.BaseShieldPoints;
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
			unit.CurrentShieldPoints[face] = target;
			assigned += target;
		}

		var leftover = total - assigned;
		while (leftover > 0)
		{
			var progress = false;
			foreach (var face in faces)
			{
				var room = max[face] - unit.CurrentShieldPoints[face];
				if (room <= 0)
					continue;

				var add = System.Math.Min(room, leftover);
				unit.CurrentShieldPoints[face] += add;
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
