using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Presentation.Ui;

public enum EPlayerMode
{
	Move,
	Missile,
	Flak,
	Railgun,
}

public static class CombatHints
{
	public static string BuildHint(
		EPlayerMode mode,
		State unit,
		int missilesRemaining,
		int queuedActionCount,
		Unit? railgunTarget,
		EMissileMount? missileMount,
		int missileRange,
		Coord? missileCenter,
		bool missileInRange,
		int moveRingCount = 0,
		int activeRingIndex = 0,
		string? activeRingHint = null)
	{
		var ap = unit.ActionPoints;
		var status = $"HP {unit.Hp}  |  {MovementSelection.FormatMomentum(unit)}  |  AP {ap}";
		var queuedSuffix = queuedActionCount > 0
			? $"  |  queued: {queuedActionCount}  |  Ctrl/Cmd+Z undo"
			: "  |  Ctrl/Cmd+Z undo";

		return mode switch
		{
			EPlayerMode.Move =>
				$"Mode: Move  |  {status}  |  missiles {missilesRemaining}/{CombatConfig.MissilesPerTurn}"
				+ (moveRingCount > 0 && activeRingHint is not null
					? $"  |  Q / E / wheel: cycle ring  |  Ring {activeRingIndex + 1}/{moveRingCount} ({activeRingHint})"
					: moveRingCount > 0
						? $"  |  Q / E / wheel: cycle ring  |  Ring {activeRingIndex + 1}/{moveRingCount}"
						: "")
				+ $"  |  click path to queue{queuedSuffix}",
			EPlayerMode.Missile =>
				$"Mode: {MountLabel(missileMount)}  |  {status}  |  {missilesRemaining}/{CombatConfig.MissilesPerTurn} left  |  range {missileRange} ({CombatConfig.ForeMissileMinRange}-{CombatConfig.ForeMissileMaxRange}, scroll)"
				+ (missileCenter is Coord center
					? missileInRange ? $"  |  center {center}" : $"  |  center {center} OUT OF ARC"
					: "  |  click arc cell  |  Esc: cancel")
				+ queuedSuffix,
			EPlayerMode.Flak =>
				$"Mode: Flak  |  {status}  |  click port or starboard arc  |  Esc: cancel{queuedSuffix}",
			EPlayerMode.Railgun =>
				$"Mode: Railgun (target M0)  |  {status}"
				+ (railgunTarget is not null
					? $"  |  target {railgunTarget.State.Id}"
					: "  |  click enemy at momentum 0")
				+ queuedSuffix,
			_ => status,
		};
	}

	private static string MountLabel(EMissileMount? mount) =>
		mount switch
		{
			EMissileMount.Fore => "Fore Missile",
			null => "Missile",
			_ => mount.ToString()!,
		};
}
