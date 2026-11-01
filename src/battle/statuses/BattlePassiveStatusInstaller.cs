using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Statuses;

public static class BattlePassiveStatusInstaller
{
	private static readonly EPilotCapabilityId[] PassiveCapabilities =
	[
		EPilotCapabilityId.ScrapDroneSwarmDamageBonus,
		EPilotCapabilityId.HealingSkillRangeBonus,
		EPilotCapabilityId.LightningShieldDamageBonus,
		EPilotCapabilityId.AdjacentDamageBonus,
		EPilotCapabilityId.ReinforcedHull,
		EPilotCapabilityId.InTheFray,
		EPilotCapabilityId.StationaryDamageDeflection,
		EPilotCapabilityId.OneWithTheVoid,
	];

	public static void InstallFromCapabilities(State state, ActorRuntime runtime)
	{
		ArgumentNullException.ThrowIfNull(state);
		ArgumentNullException.ThrowIfNull(runtime);

		foreach (var capability in PassiveCapabilities)
		{
			BattleStatuses.RemoveInstance(
				state.Statuses,
				BattleStatuses.PassiveInstanceId(capability.ToString(), state.Id));
			if (runtime.PilotCapabilities.Contains(capability))
				state.Statuses.Add(Create(capability, state.Id));
		}
	}

	public static BattleStatus Create(EPilotCapabilityId capability, string actorId)
	{
		var instanceId = BattleStatuses.PassiveInstanceId(capability.ToString(), actorId);
		return capability switch
		{
			EPilotCapabilityId.ScrapDroneSwarmDamageBonus => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [BattleActionTags.Attack, BattleActionTags.ScrapDrone],
				Polarity = EStatusPolarity.Buff,
				RewriteOutgoing = (_, _, effect) =>
					effect is ApplyAreaDamageEffect area
						? area.WithAdditionalDamage(PilotSkillPercents.ScrapDrone.SwarmAdditionalDamage)
						: effect,
			},
			EPilotCapabilityId.HealingSkillRangeBonus => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [BattleActionTags.Heal],
				Polarity = EStatusPolarity.Buff,
				RewriteArea = (area, tags) =>
					tags.Contains(BattleActionTags.Heal) && area is ScrapDroneSwarmSpec swarm
						? swarm with
						{
							BurstRange = swarm.BurstRange
								+ PilotSkillPercents.HelpABotterOut.HealingRangeBonusCells,
						}
						: area,
			},
			EPilotCapabilityId.LightningShieldDamageBonus => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [BattleActionTags.Attack, BattleActionTags.Lightning],
				Polarity = EStatusPolarity.Buff,
				RewriteOutgoing = (_, _, effect) =>
					effect switch
					{
						ApplyAreaDamageEffect area => area.WithShieldDamageBonusPercent(
							PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent),
						ApplyChainLightningDamageEffect chain => chain.WithShieldDamageBonusPercent(
							PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent),
						_ => effect,
					},
			},
			EPilotCapabilityId.AdjacentDamageBonus => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [BattleActionTags.Attack],
				Polarity = EStatusPolarity.Buff,
				RewriteOutgoing = (_, _, effect) =>
					effect is ApplyAreaDamageEffect area
						? area.WithAdjacentHitDamagePercent(
							PilotSkillPercents.DodgeThis.BonusHitDamagePercent,
							PilotSkillPercents.DodgeThis.BonusRangeCells)
						: effect,
			},
			EPilotCapabilityId.ReinforcedHull => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteHullDamage = hullDamage =>
					Defense.ScalePercent(
						hullDamage,
						PilotSkillPercents.ReinforcedHull.HullDamageTakenPercent),
			},
			EPilotCapabilityId.InTheFray => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteIncomingHit = (world, target, _, hit) =>
					hit.Damage <= 0
					|| world.CountUnitsInRange(
						target.State.Id,
						EUnitRelation.Opponent,
						PilotSkillPercents.InTheFray.MinimumDistanceCells)
						< PilotSkillPercents.InTheFray.MinimumEnemyCount
						? hit
						: Defense.ScaleHitDamageTaken(
							hit,
							PilotSkillPercents.InTheFray.DamageTakenPercent),
			},
			EPilotCapabilityId.StationaryDamageDeflection => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 200,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteIncomingHit = (_, target, cause, hit) =>
				{
					var unit = target.State;
					if (hit.Damage <= 0 || unit.DefensiveManeuversMpUsed != 0)
						return hit;
					if (!Defense.RollsDeflection(
						unit,
						unit.DefensiveManeuversRollIndex++,
						cause,
						PilotSkillPercents.DefensiveManeuvers.DeflectChancePercent))
						return hit;
					return Defense.RetainHit(
						hit,
						PilotSkillPercents.DefensiveManeuvers.DeflectDamageTakenPercent);
				},
			},
			EPilotCapabilityId.OneWithTheVoid => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
			},
			_ => throw new InvalidDataException($"Capability '{capability}' has no passive status."),
		};
	}
}
