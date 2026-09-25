using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Units;

public static class Factory
{
	public static Unit Create(BattleSpawn spawn) =>
		Create(
			spawn.Ship,
			spawn.Team,
			spawn.Position,
			spawn.ExecutionAgent,
			spawn.Fore,
			spawn.Dorsal);

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
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent,
		Coord fore,
		Coord dorsal,
		string parentId = BattleActorIds.Rules)
	{
		var id = ResolveId(ship);
		var state = State.FromShipInstance(ship, position, fore, dorsal, parentId);
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
