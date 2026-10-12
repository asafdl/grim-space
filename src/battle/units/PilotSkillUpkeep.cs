using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

internal static class PilotSkillUpkeep
{
	public static void ApplyPendingShieldRestore(State actor)
	{
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var pending = actor.PendingShieldRestore[face];
			if (pending <= 0)
				continue;

			var max = actor.Loadout.MaxShieldPoints[face];
			actor.ShieldPoints[face] = System.Math.Min(
				max,
				actor.ShieldPoints[face] + pending);
		}

		actor.PendingShieldRestore.Fill(0);
	}
}
