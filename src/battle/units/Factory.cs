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

		var heatGainReductionStacks = pilot.CapabilityGrants.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.HeatGainReduction);
		if (heatGainReductionStacks <= 0 || !state.HeatProfile.Participates)
			return;

		var reductionPercent = PilotSkillPercents.PodRacer.HeatGainReductionPercent;
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(reductionPercent, 100);

		var profile = state.HeatProfile;
		var retainedFraction = (100 - reductionPercent) / 100f;
		var gainMultiplier = profile.GainMultiplier;
		for (var i = 0; i < heatGainReductionStacks; i++)
			gainMultiplier *= retainedFraction;

		state.HeatProfile = new HeatSpec(
			profile.Capacity,
			profile.ClearPerTurn,
			gainMultiplier);
	}

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
