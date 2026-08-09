using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Ui;

public enum EPlayerMode
{
	Move,
	Flak,
	Railgun,
	Torpedo,
}

public static class CombatHints
{
	public static string BuildHint(
		EPlayerMode mode,
		State unit,
		int queuedActionCount)
	{
		var ap = unit.ActionPoints;
		var status = $"Hull {unit.HullPoints}  |  {MovementSelection.FormatMomentum(unit)}  |  AP {ap}";
		var queuedSuffix = queuedActionCount > 0
			? $"  |  queued: {queuedActionCount}  |  Ctrl/Cmd+Z undo"
			: "  |  Ctrl/Cmd+Z undo";

		return mode switch
		{
			EPlayerMode.Move =>
				$"Mode: Move  |  {status}  |  click path to queue  |  1-4: abilities  |  Q/E: spin/yaw  |  R/F: reset/focus camera{queuedSuffix}",
			EPlayerMode.Flak =>
				$"Mode: Flak  |  {status}  |  click port or starboard arc  |  1-4: abilities  |  Esc: cancel{queuedSuffix}",
			EPlayerMode.Railgun =>
				$"Mode: Railgun  |  {status}  |  click fore burst to fire  |  1-4: abilities  |  Q/E: spin/yaw  |  Esc: cancel{queuedSuffix}",
			EPlayerMode.Torpedo =>
				$"Mode: Torpedo  |  {status}  |  click mount cube to fire  |  1-4: abilities  |  Esc: cancel{queuedSuffix}",
			_ => status,
		};
	}
}
