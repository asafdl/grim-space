using System.Text.Json;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using BattleUnitState = GrimSpace.Battle.Units.State;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Specs;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Runtime;
using MapUnitType = GrimSpace.World.StarSystem.Units.EType;
using StarState = GrimSpace.World.StarSystem.Units.State;
using BattleFactory = GrimSpace.Battle.Units.Factory;

namespace GrimSpace.Run.Persistence;

public static class SaveDtoMapper
{
	public static StarSystemRuntimeDto CaptureRuntime(
		string actorId,
		ActorRuntime runtime,
		PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(runtime);
		ArgumentNullException.ThrowIfNull(registry);
		return new StarSystemRuntimeDto(
			actorId,
			runtime.CachedPath is null
				? null
				: JsonSerializer.SerializeToElement(runtime.CachedPath, registry.Options),
			runtime.PendingCompletion is null
				? null
				: registry.Write(runtime.PendingCompletion),
			runtime.PendingCompletionTick,
			runtime.JourneyIdSequence);
	}

	public static void RestoreRuntime(
		StarSystemRuntimeDto dto,
		ActorRuntime runtime,
		PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(dto);
		ArgumentNullException.ThrowIfNull(runtime);
		ArgumentNullException.ThrowIfNull(registry);
		runtime.CachedPath = dto.CachedPath is { } path
			? JsonSerializer.Deserialize<TransitPath>(path, registry.Options)
			: null;
		runtime.PendingCompletion = dto.PendingCompletion is { } pending
			? registry.Read(pending) as IAction
			: null;
		runtime.PendingCompletionTick = dto.PendingCompletionTick;
		runtime.JourneyIdSequence = dto.JourneyIdSequence;
	}

	public static BattleEncounterSaveDto CaptureBattleEncounter(BattleEncounter encounter)
	{
		ArgumentNullException.ThrowIfNull(encounter);
		return ReflectionJson.Map<BattleEncounterSaveDto>(new
		{
			encounter.Seed,
			encounter.Id,
			encounter.Objective,
			Spawns = encounter.Spawns.Select(spawn =>
				ReflectionJson.Map<BattleEncounterSpawnDto>(new
				{
					Ship = CaptureShip(spawn.Ship),
					spawn.Team,
					spawn.Position,
					spawn.Fore,
					spawn.Dorsal,
					AgentKind = BattleAgentFactory.KindOf(spawn.ExecutionAgent),
				})).ToArray(),
			Hazards = encounter.WorldHazards.Select(hazard =>
				ReflectionJson.Map<BattleEncounterHazardDto>(new
				{
					hazard.Origin,
					Cells = hazard.Cells.ToArray(),
				})).ToArray(),
		});
	}

	public static BattleEncounter RestoreBattleEncounter(BattleEncounterSaveDto dto)
	{
		ArgumentNullException.ThrowIfNull(dto);
		return new BattleEncounter
		{
			Seed = dto.Seed,
			Id = dto.Id,
			Objective = dto.Objective,
			Spawns = dto.Spawns.Select(spawn => new BattleSpawn
			{
				Ship = RestoreShip(spawn.Ship),
				Team = spawn.Team,
				Position = spawn.Position,
				Fore = spawn.Fore,
				Dorsal = spawn.Dorsal,
				ExecutionAgent = BattleAgentFactory.Create(spawn.AgentKind),
			}).ToArray(),
			WorldHazards = dto.Hazards.Select(hazard => new BattleHazardSpawn
			{
				Origin = hazard.Origin,
				Cells = hazard.Cells.ToHashSet(),
			}).ToArray(),
		};
	}

	public static ShipPersistenceDto CaptureShip(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var loadout = ship.Loadout;
		return ReflectionJson.Map<ShipPersistenceDto>(new
		{
			ship.Id,
			Chassis = ship.Spec.Chassis,
			ship.HullPoints,
			ShieldPoints = CaptureFaces(ship.ShieldPoints),
			MaxHullPoints = loadout.MaxHullPoints,
			MaxShieldPoints = CaptureFaces(loadout.MaxShieldPoints),
			ShieldUpgradeTiers = CaptureFaces(loadout.ShieldUpgradeTiers),
			loadout.HullUpgradeTier,
			InstalledAbilities = loadout.InstalledAbilities.Select(installed => new
			{
				MountedOn = installed.MountedOn,
				Spec = CaptureAbility(installed.Spec),
			}).ToArray(),
		});
	}

	public static ShipInstance RestoreShip(ShipPersistenceDto dto)
	{
		ArgumentNullException.ThrowIfNull(dto);
		var spec = ShipCatalog.SpecFor(dto.Chassis);
		var installed = dto.InstalledAbilities
			.Select(ability => new InstalledAbility(
				RestoreAbility(ability.Spec),
				ability.MountedOn))
			.ToArray();
		var loadout = ShipLoadout.Create(
			spec,
			dto.MaxHullPoints,
			RestoreFaces(dto.MaxShieldPoints),
			installed,
			RestoreFaces(dto.ShieldUpgradeTiers),
			dto.HullUpgradeTier);
		return new ShipInstance(
			dto.Id,
			spec,
			loadout,
			dto.HullPoints,
			RestoreFaces(dto.ShieldPoints));
	}

	public static BattleUnitStateDto CaptureBattleUnitState(BattleUnitState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var ship = CaptureShip(new ShipInstance(
				state.Id,
				ShipCatalog.SpecFor(state.Type),
				state.Loadout.DeepCopy(),
				state.HullPoints,
				state.ShieldPoints.Clone()));
		return ReflectionJson.Map<BattleUnitStateDto>(new
		{
			Ship = ship,
			state.Position,
			state.Fore,
			state.Dorsal,
			state.Starboard,
			state.ActionPoints,
			state.FuelRemaining,
			state.ParentId,
			state.ApPenaltyNextTurn,
			MaxAp = state.Stats.MaxAp,
			state.Projectile,
			MountRuntime = state.MountRuntime.Select(pair => new
			{
				Kind = pair.Key.Kind,
				MountedOn = pair.Key.Facet,
				pair.Value.UsesRemaining,
				pair.Value.CooldownRemaining,
			}).ToArray(),
		});
	}

	public static BattleUnitState RestoreBattleUnitState(BattleUnitStateDto dto)
	{
		ArgumentNullException.ThrowIfNull(dto);
		var state = BattleUnitState.FromShipInstance(
			RestoreShip(dto.Ship),
			dto.Position,
			dto.Fore,
			dto.Dorsal,
			dto.ParentId);
		state.Starboard = dto.Starboard;
		state.ActionPoints = dto.ActionPoints;
		state.FuelRemaining = dto.FuelRemaining;
		state.ApPenaltyNextTurn = dto.ApPenaltyNextTurn;
		state.Projectile = dto.Projectile;
		state.Stats = new Stats { MaxAp = dto.MaxAp };
		foreach (var runtime in dto.MountRuntime)
			state.MountRuntime[new AbilityMount(runtime.Kind, runtime.MountedOn)] =
				new MountRuntimeCounters
				{
					UsesRemaining = runtime.UsesRemaining,
					CooldownRemaining = runtime.CooldownRemaining,
				};
		return state;
	}

	public static TimelineSnapshotDto CaptureTimeline(
		TimelineSnapshot snapshot,
		PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(snapshot);
		ArgumentNullException.ThrowIfNull(registry);
		return new TimelineSnapshotDto(
			snapshot.CurrentTick,
			Flatten(snapshot.History, registry),
			Flatten(snapshot.Pending, registry));
	}

	public static TimelineSnapshot RestoreTimeline(
		TimelineSnapshotDto dto,
		PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(dto);
		ArgumentNullException.ThrowIfNull(registry);
		var history = Decode(dto.History, registry, pendingOnly: false);
		var pending = Decode(dto.Pending, registry, pendingOnly: true)
			.ToDictionary(
				pair => pair.Key,
				pair => (IReadOnlyList<IAction>)pair.Value.Cast<IAction>().ToArray());
		return new TimelineSnapshot(dto.CurrentTick, history, pending);
	}

	public static BattleWorldSaveDto CaptureBattleWorld(
		BattleWorld world,
		PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(world);
		var units = world.UnitRegistry.All.Select(unit =>
		{
			var state = unit.State;
			var ship = new ShipInstance(
				state.Id,
				ShipCatalog.SpecFor(state.Type),
				state.Loadout.DeepCopy(),
				state.HullPoints,
				state.ShieldPoints.Clone());
			return ReflectionJson.Map<BattleUnitSaveDto>(new
			{
				Ship = CaptureShip(ship),
				unit.Team,
				AgentKind = BattleAgentFactory.KindOf(unit.ExecutionAgent),
				state.Position,
				state.Fore,
				state.Dorsal,
				state.Starboard,
				state.ActionPoints,
				state.FuelRemaining,
				state.ParentId,
				state.ApPenaltyNextTurn,
				MaxAp = state.Stats.MaxAp,
				state.Projectile,
				MountRuntime = state.MountRuntime.Select(pair => new
				{
					Kind = pair.Key.Kind,
					MountedOn = pair.Key.Facet,
					pair.Value.UsesRemaining,
					pair.Value.CooldownRemaining,
				}).ToArray(),
			});
		}).ToArray();
		var hazards = world.Hazards.Select(hazard =>
			ReflectionJson.Map<HazardSaveDto>(new
			{
				hazard.Id,
				hazard.ActorId,
				hazard.Center,
				hazard.Frame,
				Cells = hazard.Cells.ToArray(),
				hazard.Passable,
				hazard.Damage,
				hazard.Kind,
			})).ToArray();

		return ReflectionJson.Map<BattleWorldSaveDto>(new
		{
			world.BattleId,
			world.Objective,
			GridWidth = world.Grid.Width,
			GridHeight = world.Grid.Height,
			GridDepth = world.Grid.Depth,
			BlockedCells = world.BlockedCells.ToArray(),
			EngagedShipIds = world.EngagedShipIds.ToArray(),
			BattleResult = world.battleResult,
			Units = units,
			Hazards = hazards,
			Timeline = CaptureTimeline(world.Timeline.ToSnapshot(), registry),
		});
	}

	public static StarMapSaveDto CaptureStarMap(StarMap map, PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(registry);
		return ReflectionJson.Map<StarMapSaveDto>(new
		{
			Blueprint = ReflectionJson.Map<StarMapBlueprintDto>(map.Blueprint, registry.Options),
			Pois = map.PointsOfInterest.Select(poi => new
			{
				poi.Id,
				Kind = poi.GetType().Name,
				poi.DisplayName,
				poi.Radius,
				poi.LogicalRole,
				PhysicalForm = (poi as AdministrativeCore)?.PhysicalForm,
				poi.Center,
				poi.Facade,
				Facilities = poi.Facilities
					.Select(facility => new FacilitySaveDto(
						facility.Id,
						facility.DisplayName,
						facility.PresentationAnchor,
						facility.ScenePath,
						facility.Operators))
					.ToArray(),
				poi.NextAvailableTaskTick,
				TemporaryRoles = poi.OperatorTemporaryRoles.Snapshot()
					.Select(role => ReflectionJson.Map<TemporaryOperatorRoleDto>(
						role,
						registry.Options))
					.ToArray(),
			}).ToArray(),
			Landmarks = map.NavigationLandmarks,
			Docks = map.DocksById.Values.ToArray(),
			Routes = map.RoutesById.Values.ToArray(),
			Fleets = map.FleetRegistry.All.Select(CaptureFleet).ToArray(),
			Contracts = map.ContractRegistry.Snapshot()
				.Select(entry => CaptureContract(entry, registry)).ToArray(),
			MaxPendingContracts = map.ContractRegistry.MaxPending,
			ContractIssuerCooldowns = map.ContractRegistry.IssuerGenerationCooldownsSnapshot(),
			StoryObjectives = map.StoryObjectives.Active,
			Resources = map.PlayerResources.EnumerateBalances()
				.Select(pair => ReflectionJson.Map<ResourceBalanceDto>(pair, registry.Options))
				.ToArray(),
			map.WaitingForPlayerInput,
			map.ActiveNarrativeId,
			Timeline = CaptureTimeline(map.Timeline.ToSnapshot(), registry),
		}, registry.Options);
	}

	public static StarMap RestoreStarMap(StarMapSaveDto dto, PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(dto);
		ArgumentNullException.ThrowIfNull(registry);
		var templates = dto.Blueprint.SupplyPlan.CreatePoiTemplates(dto.Blueprint.Seed)
			.ToDictionary(poi => poi.Id, StringComparer.Ordinal);
		var pois = dto.Pois.Select(saved =>
		{
			var poi = PoiRestore.FromDto(saved, dto.Blueprint.SupplyPlan);
			if (!string.Equals(poi.Id, saved.Id, StringComparison.Ordinal)
				|| !string.Equals(poi.DisplayName, saved.DisplayName, StringComparison.Ordinal)
				|| poi.Radius != saved.Radius
				|| poi.LogicalRole != saved.LogicalRole)
				throw new InvalidDataException(
					$"Saved POI '{saved.Id}' has inconsistent structural data.");
			poi.Facade = saved.Facade;
			poi.NextAvailableTaskTick = saved.NextAvailableTaskTick;
			poi.RestoreOperatorTemporaryRoles(saved.TemporaryRoles.Select(role =>
				(role.FacilityId, role.OperatorName, role.Role, role.SourceId)));
			return poi;
		}).ToArray();
		var docks = dto.Docks.ToDictionary(dock => dock.Id, StringComparer.Ordinal);
		var docksByPoi = docks.Values.ToDictionary(dock => dock.PoiId, StringComparer.Ordinal);
		var routes = dto.Routes.ToDictionary(route => route.Id, StringComparer.Ordinal);
		var blueprint = new StarSystemBlueprint(
			dto.Blueprint.Seed,
			dto.Blueprint.Width,
			dto.Blueprint.Height,
			dto.Blueprint.SystemClass,
			dto.Blueprint.ControllingFaction,
			dto.Blueprint.SupplyPlan,
			templates.Values.ToArray(),
			[],
			default!);
		var fleets = new FleetRegistry();
		foreach (var saved in dto.Fleets)
			fleets.Add(RestoreFleet(saved));
		var contracts = new ContractRegistry();
		contracts.RestoreSnapshot(
			dto.Contracts.Select(contract => RestoreContract(contract, registry)).Select(entry =>
				(entry.Contract, entry.State, entry.ExpiresAtTick)),
			dto.MaxPendingContracts,
			dto.ContractIssuerCooldowns);
		var objectives = new StoryObjectiveRegistry();
		foreach (var objective in dto.StoryObjectives)
			objectives.Add(objective);
		var resources = new PlayerResources();
		foreach (var resource in dto.Resources)
			if (resource.Balance > 0)
				resources.TryApply(ResourceBundle.Of(resource.Id, resource.Balance));
		var terrain = PathfindingTerrain.Create(
			dto.Blueprint.Width,
			dto.Blueprint.Height,
			routes.Values,
			pois,
			dto.Landmarks,
			docks.Values);
		return new StarMap(
			blueprint,
			pois,
			dto.Landmarks,
			Timeline.From(RestoreTimeline(dto.Timeline, registry)),
			docks,
			docksByPoi,
			routes,
			fleets,
			contracts,
			objectives,
			resources,
			terrain,
			waitingForPlayerInput: dto.WaitingForPlayerInput,
			activeNarrativeId: dto.ActiveNarrativeId);
	}

	private static StarMapFleetDto CaptureFleet(Fleet fleet) =>
		new(
			JsonSerializer.SerializeToElement(new
			{
				fleet.State.Id, fleet.State.Type, fleet.State.Faction,
				fleet.State.DockedAtDockId,
				fleet.State.IdleCoord, fleet.State.PatrolOrigin, fleet.State.PatrolRadius,
				fleet.State.Phase, fleet.State.ChoreDockIds,
				fleet.State.ChoreIndex, fleet.State.SpeedPerTick,
				fleet.State.EngageRadius, fleet.State.VisionRadius,
				fleet.State.WorkStartTick, fleet.State.SpawnWorkPoiId,
				fleet.State.SpawnWorkRemainingTicks, fleet.State.CurrentEngagement,
				fleet.State.Journey.JourneyId, fleet.State.Journey.Origin,
				fleet.State.Journey.Destination, fleet.State.Journey.StartTick,
				fleet.State.TravelTarget, fleet.State.PendingWreckContractId,
			}),
			fleet.Members.Select(member => member.Id).ToArray(),
			fleet.Registrations);

	private static Fleet RestoreFleet(StarMapFleetDto dto)
	{
		var state = JsonSerializer.Deserialize<StarMapStateData>(dto.State)
			?? throw new InvalidDataException("Fleet state is missing.");
		var restored = new StarState
		{
			Id = state.Id, Type = state.Type, Faction = state.Faction,
			DockedAtDockId = state.DockedAtDockId,
			IdleCoord = state.IdleCoord, PatrolOrigin = state.PatrolOrigin,
			PatrolRadius = state.PatrolRadius, Phase = state.Phase,
			ChoreDockIds = state.ChoreDockIds, ChoreIndex = state.ChoreIndex,
			SpeedPerTick = state.SpeedPerTick, EngageRadius = state.EngageRadius,
			VisionRadius = state.VisionRadius, WorkStartTick = state.WorkStartTick,
		};
		restored.SpawnWorkPoiId = state.SpawnWorkPoiId;
		restored.SpawnWorkRemainingTicks = state.SpawnWorkRemainingTicks;
		restored.CurrentEngagement = state.CurrentEngagement;
		restored.TravelTarget = state.TravelTarget;
		restored.PendingWreckContractId = state.PendingWreckContractId;
		restored.Journey.JourneyId = state.JourneyId;
		restored.Journey.Origin = state.Origin;
		restored.Journey.Destination = state.Destination;
		restored.Journey.StartTick = state.StartTick;
		return new Fleet(restored, dto.Members.Select(id => new FleetMember(id)), dto.Registrations);
	}

	internal static StarMapContractDto CaptureContract(
		(Contract Contract, ContractState? State, int? ExpiresAtTick) entry,
		PersistenceRegistry registry) =>
		ReflectionJson.Map<StarMapContractDto>(new
		{
			entry.Contract.Id,
			ObjectiveType = entry.Contract.Objective.GetType().Name,
			Objective = JsonSerializer.SerializeToElement(
				entry.Contract.Objective,
				entry.Contract.Objective.GetType(),
				registry.Options),
			entry.Contract.Danger,
			entry.Contract.IssuerFaction,
			entry.Contract.IssuerPoiId,
			entry.Contract.Terms,
			entry.Contract.Narrative,
			entry.Contract.IsStoryObjective,
			entry.State,
			entry.ExpiresAtTick,
		}, registry.Options);

	internal static (Contract Contract, ContractState? State, int? ExpiresAtTick) RestoreContract(
		StarMapContractDto dto,
		PersistenceRegistry registry)
	{
		IContractObjective objective = dto.ObjectiveType switch
		{
			nameof(HuntObjective) => JsonSerializer.Deserialize<HuntObjective>(
				dto.Objective,
				registry.Options)!,
			nameof(DeliveryObjective) => JsonSerializer.Deserialize<DeliveryObjective>(
				dto.Objective,
				registry.Options)!,
			nameof(WreckageObjective) => JsonSerializer.Deserialize<WreckageObjective>(
				dto.Objective,
				registry.Options)!,
			_ => throw new InvalidDataException($"Unknown contract objective '{dto.ObjectiveType}'."),
		};
		return (
			new Contract(
				dto.Id, objective, dto.Danger, dto.IssuerFaction, dto.IssuerPoiId,
				dto.Terms, dto.Narrative, ContractFactory.ObjectiveMetFor(objective),
				dto.IsStoryObjective),
			dto.State,
			dto.ExpiresAtTick);
	}

	private sealed record StarMapStateData(
		string Id, MapUnitType Type, EFaction Faction,
		string DockedAtDockId, Coord IdleCoord, Coord PatrolOrigin, int PatrolRadius, EPhase Phase,
		IReadOnlyList<string> ChoreDockIds, int ChoreIndex, double SpeedPerTick,
		double EngageRadius, double VisionRadius, int WorkStartTick,
		string? SpawnWorkPoiId, int SpawnWorkRemainingTicks,
		Engagement? CurrentEngagement, long JourneyId, Coord Origin,
		Coord Destination, int StartTick, TravelTarget TravelTarget,
		string PendingWreckContractId);

	public static BattleWorld RestoreBattleWorld(
		BattleWorldSaveDto dto,
		PersistenceRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(dto);
		var grid = new Grid(dto.GridWidth, dto.GridHeight, dto.GridDepth);
		var units = dto.Units.Select(unitDto =>
		{
			var ship = RestoreShip(unitDto.Ship);
			var state = BattleUnitState.FromShipInstance(
				ship,
				unitDto.Position,
				unitDto.Fore,
				unitDto.Dorsal,
				unitDto.ParentId);
			state.Starboard = unitDto.Starboard;
			state.ActionPoints = unitDto.ActionPoints;
			state.FuelRemaining = unitDto.FuelRemaining;
			state.ApPenaltyNextTurn = unitDto.ApPenaltyNextTurn;
			state.Projectile = unitDto.Projectile;
			state.Stats = new Stats { MaxAp = unitDto.MaxAp };
			foreach (var runtime in unitDto.MountRuntime)
				state.MountRuntime[new AbilityMount(runtime.Kind, runtime.MountedOn)] =
					new MountRuntimeCounters
					{
						UsesRemaining = runtime.UsesRemaining,
						CooldownRemaining = runtime.CooldownRemaining,
					};
			return BattleFactory.Create(state, unitDto.Team, unitDto.AgentKind);
		}).ToArray();
		var nonUnits = dto.Hazards.ToDictionary(
			hazard => hazard.Id,
			hazard => (NonUnit)new Hazard
			{
				Id = hazard.Id,
				ActorId = hazard.ActorId,
				Center = hazard.Center,
				Frame = hazard.Frame,
				Cells = hazard.Cells.ToHashSet(),
				Passable = hazard.Passable,
				Damage = hazard.Damage,
				Kind = hazard.Kind,
			});
		var world = BattleWorld.FromLive(
			units,
			nonUnits,
			grid,
			dto.BlockedCells.ToHashSet(),
			dto.BattleId,
			dto.Objective,
			dto.EngagedShipIds.ToHashSet(StringComparer.Ordinal),
			Timeline.From(RestoreTimeline(dto.Timeline, registry)));
		world.battleResult = dto.BattleResult;
		return world;
	}

	private static AbilitySpecDto CaptureAbility(AbilitySpec spec) =>
		new(spec.Kind.ToString(), JsonSerializer.SerializeToElement(spec, spec.GetType()));

	private static AbilitySpec RestoreAbility(AbilitySpecDto dto)
	{
		if (!Enum.TryParse<EAbilityKind>(dto.Kind, ignoreCase: false, out var kind))
			throw new InvalidDataException($"Unknown ability kind '{dto.Kind}'.");

		return kind switch
		{
			EAbilityKind.ScrapDroneSwarm => Deserialize<ScrapDroneSwarmSpec>(dto.Data),
			EAbilityKind.LightningCannon => Deserialize<LightningCannonSpec>(dto.Data),
			EAbilityKind.MinerBay => new MinerBaySpec(
				dto.Data.GetProperty("CooldownTurns").GetInt32(),
				RepurposedMinerSpec.Instance,
				dto.Data.GetProperty("MaxLivingChildren").GetInt32()),
			EAbilityKind.VoidBombLauncher => Deserialize<VoidBombLauncherSpec>(dto.Data),
			_ => throw new InvalidDataException($"Unsupported ability kind '{dto.Kind}'."),
		};
	}

	private static IReadOnlyList<TimelineEntryDto> Flatten<TEntry>(
		IReadOnlyDictionary<int, IReadOnlyList<TEntry>> buckets,
		PersistenceRegistry registry)
		where TEntry : ITimelineEntry =>
		buckets.OrderBy(pair => pair.Key)
			.SelectMany(pair => pair.Value.Select(entry =>
				new TimelineEntryDto(pair.Key, registry.Write(entry!))))
			.ToArray();

	private static IReadOnlyDictionary<int, IReadOnlyList<ITimelineEntry>> Decode(
		IReadOnlyList<TimelineEntryDto> entries,
		PersistenceRegistry registry,
		bool pendingOnly)
	{
		var buckets = new Dictionary<int, List<ITimelineEntry>>();
		foreach (var entry in entries)
		{
			var decoded = registry.Read(entry.Entry);
			if (decoded is not ITimelineEntry timelineEntry
				|| (pendingOnly && timelineEntry is not IAction))
				throw new InvalidDataException(
					$"Invalid {(pendingOnly ? "pending action" : "history entry")} at tick {entry.Tick}.");
			if (!buckets.TryGetValue(entry.Tick, out var bucket))
			{
				bucket = [];
				buckets[entry.Tick] = bucket;
			}
			bucket.Add(timelineEntry);
		}

		return buckets.ToDictionary(
			pair => pair.Key,
			pair => (IReadOnlyList<ITimelineEntry>)pair.Value.ToArray());
	}

	private static FaceShieldDto CaptureFaces(FaceShieldPoints points) =>
		new(Enum.GetValues<ESpatialOrientation>().Select(face => points[face]).ToArray());

	private static FaceShieldPoints RestoreFaces(FaceShieldDto dto)
	{
		var faces = Enum.GetValues<ESpatialOrientation>();
		if (dto.Points.Length != faces.Length)
			throw new InvalidDataException("Shield face data has an invalid length.");
		var points = new FaceShieldPoints();
		for (var index = 0; index < faces.Length; index++)
			points[faces[index]] = dto.Points[index];
		return points;
	}

	private static T Deserialize<T>(JsonElement data) =>
		JsonSerializer.Deserialize<T>(data)
		?? throw new InvalidDataException($"Unable to restore '{typeof(T).Name}'.");
}
