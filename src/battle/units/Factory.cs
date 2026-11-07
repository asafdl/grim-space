using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Units;

public static class Factory
{
	private const int PilotInitiativePenalty = 10;

	public static Unit Create(BattleSpawn spawn)
	{
		var unit = Create(
			spawn.Ship,
			spawn.Team,
			spawn.Position,
			spawn.ExecutionAgent,
			spawn.Fore,
			spawn.Dorsal);
		ApplyPilotSpawnModifiers(unit.State, spawn.Pilot);
		return unit;
	}

	private static void ApplyPilotSpawnModifiers(State state, PilotBattleSnapshot? pilot)
	{
		if (pilot is null)
			return;

		if (pilot.CapabilityGrants.Any(grant =>
				grant.CapabilityId == EPilotCapabilityId.InitiativePenalty))
			state.Initiative -= PilotInitiativePenalty;

		ApplyBaseHullPenalty(state, pilot);

		if (!state.HeatProfile.Participates)
			return;

		var profile = state.HeatProfile;
		var capacity = profile.Capacity;
		var gainMultiplier = profile.GainMultiplier;
		var clearPerTurn = profile.ClearPerTurn;

		var heatGainReductionStacks = pilot.CapabilityGrants.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.HeatGainReduction);
		if (heatGainReductionStacks > 0)
		{
			var reductionPercent = PilotSkillPercents.PodRacer.HeatGainReductionPercent;
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(reductionPercent, 100);
			var retainedFraction = (100 - reductionPercent) / 100f;
			for (var i = 0; i < heatGainReductionStacks; i++)
				gainMultiplier *= retainedFraction;
		}

		var heatGainBonusStacks = pilot.CapabilityGrants.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.HeatGainBonus);
		if (heatGainBonusStacks > 0)
		{
			var gainBonusPercent =
				heatGainBonusStacks * PilotSkillPercents.SynthSinger.HeatGainBonusPercent;
			gainMultiplier *= (100 + gainBonusPercent) / 100f;
		}

		var heatClearBonusStacks = pilot.CapabilityGrants.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.HeatClearBonus);
		if (heatClearBonusStacks > 0)
		{
			var clearBonusPercent =
				heatClearBonusStacks * PilotSkillPercents.CyberJunkie.HeatClearBonusPercent;
			clearPerTurn = ScalePercent(clearPerTurn, 100 + clearBonusPercent);
		}

		var heatCapacityBonusStacks = pilot.CapabilityGrants.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.HeatCapacityBonus);
		if (heatCapacityBonusStacks > 0 && capacity is > 0)
		{
			var capacityBonusPercent =
				heatCapacityBonusStacks * PilotSkillPercents.SynthSinger.HeatCapacityBonusPercent;
			capacity = ScalePercent(capacity.Value, 100 + capacityBonusPercent);
		}

		if (gainMultiplier == profile.GainMultiplier
			&& clearPerTurn == profile.ClearPerTurn
			&& capacity == profile.Capacity)
			return;

		state.HeatProfile = new HeatSpec(capacity, clearPerTurn, gainMultiplier);
	}

	private static void ApplyBaseHullPenalty(State state, PilotBattleSnapshot pilot)
	{
		var stacks = pilot.CapabilityGrants.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.BaseHullPenalty);
		if (stacks <= 0)
			return;

		var retainPercent = PilotSkillPercents.BeanCounter.BaseHullPercent;
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retainPercent, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(retainPercent, 100);

		var oldBase = state.Loadout.BaseHullPoints;
		var oldCurrent = state.CurrentHullPoints;
		var newBase = oldBase;
		for (var i = 0; i < stacks; i++)
			newBase = ScalePercent(newBase, retainPercent);

		var missing = System.Math.Max(0, oldBase - oldCurrent);
		state.HullHandoffBasePoints = oldBase;
		state.Loadout = state.Loadout with { BaseHullPoints = newBase };
		state.CurrentHullPoints = System.Math.Max(0, newBase - missing);
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, MidpointRounding.AwayFromZero);

	public static Unit Create(
		ShipInstance ship,
		ETeam team,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent) =>
		Create(ship, team, position, executionAgent, Coord.Forward, Coord.Up);

	public static Unit Create(
		ShipInstance ship,
		ETeam team,
		Coord position,
		EBattleAgentKind agentKind) =>
		Create(
			ship,
			team,
			position,
			BattleAgentFactory.Create(agentKind),
			Coord.Forward,
			Coord.Up);

	public static Unit Create(
		ShipInstance ship,
		ETeam team,
		Coord position,
		EBattleAgentKind agentKind,
		Coord fore,
		Coord dorsal,
		string parentId = BattleActorIds.Rules) =>
		Create(
			ship,
			team,
			position,
			BattleAgentFactory.Create(agentKind),
			fore,
			dorsal,
			parentId);

	public static Unit Create(State state, ETeam team, EBattleAgentKind agentKind) =>
		new(state, BattleAgentFactory.Create(agentKind), team);

	public static Unit Create(
		ShipInstance ship,
		ETeam team,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent,
		Coord fore,
		Coord dorsal,
		string parentId = BattleActorIds.Rules)
	{
		var id = ResolveId(ship);
		var state = State.FromShipInstance(
			ship,
			position,
			fore,
			dorsal,
			parentId);
		return new Unit(state, executionAgent, team);
	}

	public static ShipInstance ChildFromSpawnableMount(State parent, AbilityMount mount, string childId)
	{
		var installed = parent.FindInstalled(mount.Kind, mount.Facet)
			?? throw new InvalidOperationException(
				$"No '{mount.Kind}' ability installed on facet '{mount.Facet}' for actor '{parent.Id}'.");
		if (installed.Spec is not ISpawnable spawnable)
			throw new InvalidOperationException($"Installed ability '{installed.Spec.Kind}' is not spawnable.");

		var childSpec = spawnable.ChildSpec;
		return ShipInstance.FromSpec(
			childId,
			childSpec,
			childSpec.NewDefaultLoadout());
	}

	private static string ResolveId(ShipInstance ship) =>
		string.IsNullOrWhiteSpace(ship.Id)
			? TypedIdGenerator.NextId("unit")
			: ship.Id;
}
