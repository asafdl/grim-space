using GrimSpace.Battle.Actions;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
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
		EPilotCapabilityId.ReactiveShields,
		EPilotCapabilityId.StationaryDamageDeflection,
		EPilotCapabilityId.ShieldDynamo,
		EPilotCapabilityId.ThirstForScrap,
		EPilotCapabilityId.YouLittleRat,
		EPilotCapabilityId.NothingButMetal,
		EPilotCapabilityId.SpawnedUnitDamageBonus,
		EPilotCapabilityId.OneWithTheVoid,
	];

	public static void InstallFromCapabilities(BattleWorld world, string unitId, ActorRuntime runtime)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentException.ThrowIfNullOrEmpty(unitId);
		ArgumentNullException.ThrowIfNull(runtime);

		var state = world.StateOf(unitId);
		foreach (var capability in PassiveCapabilities)
		{
			IAction? activationAction = capability switch
			{
				EPilotCapabilityId.StationaryDamageDeflection => new SyncDefensiveManeuversMpUsedAction(unitId),
				EPilotCapabilityId.ShieldDynamo => new ExpireShieldDynamoGrantsAction(unitId),
				_ => null,
			};
			BattleStatuses.RemoveInstance(
				state.Statuses,
				BattleStatuses.PassiveInstanceId(capability.ToString(), state.Id));
			if (activationAction is not null)
				world.ActivationRoundLists.RemoveAllMatching(
					unitId,
					entry => entry.Action.GetType() == activationAction.GetType());

			if (!runtime.PilotCapabilities.Contains(capability))
				continue;

			state.Statuses.Add(Create(capability, state.Id));
			if (activationAction is not null)
				world.ActivationRoundLists.Append(
					unitId,
					new ActivationRoundEntry(
						EUnitActivationPhase.Start,
						RoundsRemaining: null,
						Sticky: true,
						activationAction));
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
			EPilotCapabilityId.NothingButMetal => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteOutgoing = (_, _, effect) => effect switch
				{
					ApplyAreaDamageEffect area => area.WithUnshieldedDamageBonusPercent(
						PilotSkillPercents.NothingButMetal.DamageBonusPercent),
					ApplyChainLightningDamageEffect chain => chain.WithUnshieldedDamageBonusPercent(
						PilotSkillPercents.NothingButMetal.DamageBonusPercent),
					_ => effect,
				},
			},
			EPilotCapabilityId.SpawnedUnitDamageBonus => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteOutgoing = (_, _, effect) => effect switch
				{
					ApplyAreaDamageEffect area => area.WithSpawnedUnitDamageBonusPercent(
						PilotSkillPercents.NotFullyFormed.SpawnedUnitDamageBonusPercent),
					ApplyChainLightningDamageEffect chain => chain.WithSpawnedUnitDamageBonusPercent(
						PilotSkillPercents.NotFullyFormed.SpawnedUnitDamageBonusPercent),
					_ => effect,
				},
			},
			EPilotCapabilityId.ThirstForScrap => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteOutgoing = (_, world, effect) =>
				{
					var bonus = world.StateOf(actorId).ThirstForScrapStacks
						* PilotSkillPercents.ThirstForScrap.DamageBonusPercentPerKill;
					return effect switch
					{
						ApplyAreaDamageEffect area => area.WithAddedDamageBonusPercent(bonus),
						ApplyChainLightningDamageEffect chain => chain.WithAddedDamageBonusPercent(bonus),
						_ => effect,
					};
				},
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
			EPilotCapabilityId.YouLittleRat => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteIncomingHit = (_, target, origin, _, _, hit) =>
					hit.Damage <= 0
					|| target.State.Position.ManhattanDistanceTo(origin)
						< PilotSkillPercents.YouLittleRat.MinimumDistanceCells
						? hit
						: Defense.ScaleHitDamageTaken(hit, PilotSkillPercents.YouLittleRat.DamageTakenPercent),
			},
			EPilotCapabilityId.InTheFray => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteIncomingHit = (world, target, _, _, _, hit) =>
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
			EPilotCapabilityId.ReactiveShields => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteIncomingHit = (_, target, _, _, _, hit) =>
				{
					if (hit.Damage <= 0)
						return hit;

					var unit = target.State;
					var scaled = hit;
					for (var i = 0; i < unit.ReactiveShieldsStacks; i++)
					{
						scaled = Defense.ScaleHitDamageTaken(
							scaled,
							PilotSkillPercents.ReactiveShields.DamageTakenPercentPerStack);
					}

					unit.ReactiveShieldsStacks++;
					return scaled;
				},
			},
			EPilotCapabilityId.StationaryDamageDeflection => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 200,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
				RewriteIncomingHit = (_, target, _, _, _, hit) =>
					hit.Damage <= 0 || target.State.DefensiveManeuversMpUsed != 0
						? hit
						: Defense.ScaleHitDamageTaken(
							hit,
							PilotSkillPercents.DefensiveManeuvers.DamageTakenPercent),
			},
			EPilotCapabilityId.ShieldDynamo => new BattleStatus
			{
				InstanceId = instanceId,
				Priority = 100,
				ActionTags = [],
				Polarity = EStatusPolarity.Buff,
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
