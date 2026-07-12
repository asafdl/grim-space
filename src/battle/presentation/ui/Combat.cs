using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Ui;

public enum EPlayerMode
{
	Move,
	Missile,
	Railgun,
}

public static class CombatHints
{
	public static string ModeLabel(EPlayerMode mode) =>
		mode switch
		{
			EPlayerMode.Move => "Move",
			EPlayerMode.Missile => "Missile",
			EPlayerMode.Railgun => "Railgun",
			_ => mode.ToString(),
		};

	public static string BuildHint(
		EPlayerMode mode,
		State unit,
		int missilesRemaining,
		int plannedActionCount,
		Unit? railgunTarget,
		EMissileMount? missileMount,
		Coord? missileCenter,
		bool missileInRange)
	{
		var ap = unit.ActionPoints;
		var status = $"HP {unit.Hp}  |  {MovementSelection.FormatMomentum(unit)}  |  AP {ap}";
		var planSuffix = plannedActionCount > 0
			? $"  |  plan: {plannedActionCount}  |  Ctrl/Cmd+Z undo"
			: "  |  Ctrl/Cmd+Z undo";

		return mode switch
		{
			EPlayerMode.Move =>
				$"Mode: Move  |  {status}  |  missiles {missilesRemaining}/{CombatConfig.MissilesPerTurn}  |  click path to queue{planSuffix}",
			EPlayerMode.Missile =>
				$"Mode: {MountLabel(missileMount)}  |  {status}  |  {missilesRemaining}/{CombatConfig.MissilesPerTurn} left  |  range {MountRange(missileMount)}"
				+ (missileCenter is Coord center
					? missileInRange ? $"  |  center {center}" : $"  |  center {center} OUT OF ARC"
					: "  |  click arc cell  |  Esc: cancel")
				+ planSuffix,
			EPlayerMode.Railgun =>
				$"Mode: Railgun (target M0)  |  {status}"
				+ (railgunTarget is not null
					? $"  |  target {railgunTarget.State.Id}"
					: "  |  click enemy at momentum 0")
				+ planSuffix,
			_ => status,
		};
	}

	private static string MountLabel(EMissileMount? mount) =>
		mount switch
		{
			EMissileMount.Dorsal => "Dorsal Missile",
			null => "Missile",
			_ => mount.ToString()!,
		};

	private static int MountRange(EMissileMount? mount) =>
		mount is EMissileMount value
			? MissileMountConfig.For(value).Range
			: 0;

	public static bool CanTargetRailgun(Unit target) =>
		target.State.IsAlive
		&& target.State.MomentumLevel == CombatConfig.RailgunRequiredTargetMomentum;
}
