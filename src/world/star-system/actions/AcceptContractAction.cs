using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record AcceptContractAction(string ActorId, string ContractId)
	: IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		AcceptContractDef.Instance;
}

public sealed class AcceptContractDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static AcceptContractDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is AcceptContractAction accept
		&& world.ContractRegistry.TryGet(accept.ContractId, out _)
		&& world.ContractRegistry.IsOffered(accept.ContractId)
		&& world.UnitRegistry.TryGet(accept.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var accept = (AcceptContractAction)action;
		var contract = world.ContractRegistry.All.First(c => c.Id == accept.ContractId);

		var effects = new List<IEffect<StarMap, ActorRuntime>>();
		var spawnBindings = new Dictionary<string, List<string>>(StringComparer.Ordinal);

		if (contract.Objective is IHasSpawnGroups hasSpawnGroups)
		{
			var planned = ContractFleetPlacement.Plan(
				hasSpawnGroups.SpawnGroups,
				accept.ContractId,
				world.Seed,
				world,
				world.UnitRegistry);

			foreach (var spawn in planned)
			{
				if (!spawnBindings.TryGetValue(spawn.GroupId, out var unitIds))
				{
					unitIds = [];
					spawnBindings[spawn.GroupId] = unitIds;
				}

				unitIds.Add(spawn.UnitId);

				var combatProfile = new CombatProfile(spawn.Spawn.Danger, spawn.Spawn.Seed);
				var unit = Factory.CreatePirateFleet(
					spawn.UnitId,
					spawn.Coord,
					spawn.Spawn.Faction,
					combatProfile);
				effects.Add(new SpawnMapUnitEffect(unit));
			}
		}

		var state = new ContractState(
			accept.ContractId,
			EContractStatus.Active,
			world.Timeline.Clock.Current,
			accept.ActorId,
			spawnBindings.ToDictionary(
				entry => entry.Key,
				entry => (IReadOnlyList<string>)entry.Value,
				StringComparer.Ordinal));
		effects.Add(new ActivateContractEffect(state));
		return effects;
	}
}
