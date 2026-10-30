using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.PilotCapabilities;

public static class HealingSkillRange
{
	public static IAreaDamage ApplyRangeBonus(
		IAreaDamage area,
		State actor,
		IReadOnlyList<string> actionTags)
	{
		ArgumentNullException.ThrowIfNull(area);
		ArgumentNullException.ThrowIfNull(actor);
		ArgumentNullException.ThrowIfNull(actionTags);
		if (!actor.HealingSkillRangeBonusActive || !actionTags.Contains(BattleActionTags.Heal))
			return area;

		return area switch
		{
			ScrapDroneSwarmSpec swarm => swarm with
			{
				BurstRange = swarm.BurstRange + PilotSkillPercents.HelpABotterOut.HealingRangeBonusCells,
			},
			_ => area,
		};
	}
}
