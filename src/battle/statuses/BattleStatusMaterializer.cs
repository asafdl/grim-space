using GrimSpace.Battle.Actions;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public static class BattleStatusMaterializer
{
	private static readonly (EPilotCapabilityId Capability, Func<BattleStatus> Create)[] Passives =
	[
		(EPilotCapabilityId.ScrapDroneSwarmDamageBonus, static () => new ScrapDroneSwarmDamageBonus()),
		(EPilotCapabilityId.HealingSkillRangeBonus, static () => new HealingSkillRangeBonus()),
		(EPilotCapabilityId.LightningShieldDamageBonus, static () => new LightningShieldDamageBonus()),
		(EPilotCapabilityId.AdjacentDamageBonus, static () => new AdjacentDamageBonus()),
		(EPilotCapabilityId.ReinforcedHull, static () => new ReinforcedHull()),
		(EPilotCapabilityId.InTheFray, static () => new InTheFray()),
		(EPilotCapabilityId.ReactiveShields, static () => new ReactiveShields()),
		(EPilotCapabilityId.StationaryDamageDeflection, static () => new StationaryDamageDeflection()),
		(EPilotCapabilityId.ShieldDynamo, static () => new ShieldDynamo()),
		(EPilotCapabilityId.ThirstForScrap, static () => new ThirstForScrap()),
		(EPilotCapabilityId.YouLittleRat, static () => new YouLittleRat()),
		(EPilotCapabilityId.NothingButMetal, static () => new NothingButMetal()),
		(EPilotCapabilityId.SpawnedUnitDamageBonus, static () => new SpawnedUnitDamageBonus()),
		(EPilotCapabilityId.OneWithTheVoid, static () => new OneWithTheVoid()),
		(EPilotCapabilityId.OutgoingDamageBonus, static () => new OutgoingDamageBonus()),
		(EPilotCapabilityId.IncreasedHullDamageTaken, static () => new IncreasedHullDamageTaken()),
		(EPilotCapabilityId.SpawnedAllyDamageBonus, static () => new SpawnedAllyDamageBonus()),
		(EPilotCapabilityId.DamageResistance, static () => new DamageResistance()),
		(EPilotCapabilityId.PirateDamageBonus, static () => new PirateDamageBonus()),
		(EPilotCapabilityId.HealBonus, static () => new HealBonus()),
		(EPilotCapabilityId.HealPenalty, static () => new HealPenalty()),
	];

	public static BattleStatus Create(EPilotCapabilityId capability) =>
		Passives.First(entry => entry.Capability == capability).Create();

	public static void Materialize(BattleWorld world, string unitId, ActorRuntime runtime)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentException.ThrowIfNullOrEmpty(unitId);
		ArgumentNullException.ThrowIfNull(runtime);

		var state = world.StateOf(unitId);
		foreach (var (capability, create) in Passives)
		{
			IAction? activationAction = capability switch
			{
				EPilotCapabilityId.StationaryDamageDeflection => new SyncDefensiveManeuversMpUsedAction(unitId),
				EPilotCapabilityId.ShieldDynamo => new ExpireShieldDynamoGrantsAction(unitId),
				_ => null,
			};
			if (activationAction is not null)
				world.ActivationRoundLists.RemoveAllMatching(
					unitId,
					entry => entry.Action.GetType() == activationAction.GetType());

			if (!runtime.PilotCapabilities.Contains(capability))
			{
				RemovePassive(state, capability);
				continue;
			}

			var status = create();
			if (!state.Statuses.Any(existing => existing.GetType() == status.GetType()))
				state.Statuses.Add(status);
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

	private static void RemovePassive(State state, EPilotCapabilityId capability)
	{
		switch (capability)
		{
			case EPilotCapabilityId.ScrapDroneSwarmDamageBonus: state.RemoveStatus<ScrapDroneSwarmDamageBonus>(); break;
			case EPilotCapabilityId.HealingSkillRangeBonus: state.RemoveStatus<HealingSkillRangeBonus>(); break;
			case EPilotCapabilityId.LightningShieldDamageBonus: state.RemoveStatus<LightningShieldDamageBonus>(); break;
			case EPilotCapabilityId.AdjacentDamageBonus: state.RemoveStatus<AdjacentDamageBonus>(); break;
			case EPilotCapabilityId.ReinforcedHull: state.RemoveStatus<ReinforcedHull>(); break;
			case EPilotCapabilityId.InTheFray: state.RemoveStatus<InTheFray>(); break;
			case EPilotCapabilityId.ReactiveShields: state.RemoveStatus<ReactiveShields>(); break;
			case EPilotCapabilityId.StationaryDamageDeflection: state.RemoveStatus<StationaryDamageDeflection>(); break;
			case EPilotCapabilityId.ShieldDynamo: state.RemoveStatus<ShieldDynamo>(); break;
			case EPilotCapabilityId.ThirstForScrap: state.RemoveStatus<ThirstForScrap>(); break;
			case EPilotCapabilityId.YouLittleRat: state.RemoveStatus<YouLittleRat>(); break;
			case EPilotCapabilityId.NothingButMetal: state.RemoveStatus<NothingButMetal>(); break;
			case EPilotCapabilityId.SpawnedUnitDamageBonus: state.RemoveStatus<SpawnedUnitDamageBonus>(); break;
			case EPilotCapabilityId.OneWithTheVoid: state.RemoveStatus<OneWithTheVoid>(); break;
			case EPilotCapabilityId.OutgoingDamageBonus: state.RemoveStatus<OutgoingDamageBonus>(); break;
			case EPilotCapabilityId.IncreasedHullDamageTaken: state.RemoveStatus<IncreasedHullDamageTaken>(); break;
			case EPilotCapabilityId.SpawnedAllyDamageBonus: state.RemoveStatus<SpawnedAllyDamageBonus>(); break;
			case EPilotCapabilityId.DamageResistance: state.RemoveStatus<DamageResistance>(); break;
			case EPilotCapabilityId.PirateDamageBonus: state.RemoveStatus<PirateDamageBonus>(); break;
			case EPilotCapabilityId.HealBonus: state.RemoveStatus<HealBonus>(); break;
			case EPilotCapabilityId.HealPenalty: state.RemoveStatus<HealPenalty>(); break;
		}
	}
}
