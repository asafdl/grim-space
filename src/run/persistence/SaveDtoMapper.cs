using System.Collections.Frozen;
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
using GrimSpace.World.StarSystem.FleetSpawner;
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
			runtime.JourneyIdSequence,
			new Dictionary<string, int>(
				runtime.IgnoreUntilTickByTargetId,
				StringComparer.Ordinal),
			runtime.ActionCooldownUntilTick,
			runtime.SelectedMemberShipId);
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
		runtime.ActionCooldownUntilTick = dto.ActionCooldownUntilTick;
		runtime.SelectedMemberShipId = dto.SelectedMemberShipId;
		runtime.IgnoreUntilTickByTargetId.Clear();
		if (dto.IgnoreUntilTickByTargetId is not null)
		{
			foreach (var (targetId, ignoreUntilTick) in dto.IgnoreUntilTickByTargetId)
				runtime.IgnoreUntilTickByTargetId[targetId] = ignoreUntilTick;
		}
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
			InstalledAbilities = loadout.InstalledAbilities
				.Select(installed => new InstalledAbilityDto(
					installed.MountedOn,
					installed.Kind,
					installed.DamageUpgradeTier,
					installed.RangeUpgradeTier))
				.ToArray(),
		});
	}

	public static ShipInstance RestoreShip(ShipPersistenceDto dto)
	{
		ArgumentNullException.ThrowIfNull(dto);
		var spec = ShipCatalog.SpecFor(dto.Chassis);
		var installed = dto.InstalledAbilities
			.Select(RestoreInstalledAbility)
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
			state.Initiative,
			state.Position,
			state.Fore,
			state.Dorsal,
			state.Starboard,
			state.ActionPoints,
			state.ManeuverPoints,
			state.FuelRemaining,
			state.ParentId,
			state.ApPenaltyNextTurn,
			MaxAp = state.Maneuverability.MaxActionPoints,
			MaxMp = state.Maneuverability.MaxManeuverPoints,
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
		state.Initiative = dto.Initiative ?? state.Initiative;
		state.ActionPoints = dto.ActionPoints;
		var maneuverability = dto.Projectile?.Maneuverability() ?? state.Maneuverability;
		var maxMp = dto.MaxMp ?? maneuverability.MaxManeuverPoints;
		state.ManeuverPoints = dto.ManeuverPoints ?? maxMp;
		state.FuelRemaining = dto.FuelRemaining;
		state.ApPenaltyNextTurn = dto.ApPenaltyNextTurn;
		state.Projectile = dto.Projectile;
		state.Maneuverability = maneuverability.WithBudgets(
			dto.MaxAp,
			maxMp);
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
				state.Initiative,
				state.Position,
				state.Fore,
				state.Dorsal,
				state.Starboard,
				state.ActionPoints,
				state.ManeuverPoints,
				state.FuelRemaining,
				state.ParentId,
				state.ApPenaltyNextTurn,
				MaxAp = state.Maneuverability.MaxActionPoints,
				MaxMp = state.Maneuverability.MaxManeuverPoints,
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
		var hazards = world.NonUnits.Values.Select(CaptureNonUnit).ToArray();

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
				fleet.State.AggressionRating,
				Travel = CaptureFleetTravel(fleet.State.Travel),
				fleet.State.PatrolOrigin, fleet.State.PatrolRadius,
				fleet.State.ChoreDockIds,
				fleet.State.ChoreIndex, fleet.State.SpeedPerTick,
				fleet.State.EngageRadius, fleet.State.VisionRadius,
				fleet.State.CurrentEngagement,
				fleet.State.SpawnerSource, fleet.State.FleetSpawnerExpiresAtTick,
				fleet.State.SourceContractId, fleet.State.PursuitDirective,
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
			AggressionRating = state.AggressionRating,
			Travel = RestoreFleetTravel(state.Travel),
			PatrolOrigin = state.PatrolOrigin,
			PatrolRadius = state.PatrolRadius,
			ChoreDockIds = state.ChoreDockIds, ChoreIndex = state.ChoreIndex,
			SpeedPerTick = state.SpeedPerTick, EngageRadius = state.EngageRadius,
			VisionRadius = state.VisionRadius,
			SpawnerSource = state.SpawnerSource,
			FleetSpawnerExpiresAtTick = state.FleetSpawnerExpiresAtTick,
			SourceContractId = state.SourceContractId,
		};
		restored.PursuitDirective = state.PursuitDirective;
		restored.CurrentEngagement = state.CurrentEngagement;
		restored.TravelTarget = state.TravelTarget;
		restored.PendingWreckContractId = state.PendingWreckContractId;
		return new Fleet(
			restored,
			dto.Members.Select(id => new FleetMember(id)).ToArray(),
			dto.Registrations);
	}

	private static StarMapTravelData CaptureFleetTravel(FleetTravel travel) =>
		travel switch
		{
			FleetTravel.AtRest atRest =>
				new StarMapTravelData(atRest.Position, 0, default, default, 0),
			FleetTravel.Journey journey =>
				new StarMapTravelData(
					null,
					journey.Id,
					journey.Origin,
					journey.Destination,
					journey.StartTick),
			_ => throw new InvalidOperationException($"Unknown fleet travel type '{travel.GetType().Name}'."),
		};

	private static FleetTravel RestoreFleetTravel(StarMapTravelData travel) =>
		(travel.AtRestPosition, travel.JourneyId) switch
		{
			({ } position, 0) => new FleetTravel.AtRest(position),
			(null, not 0) => new FleetTravel.Journey(
				travel.JourneyId,
				travel.Origin,
				travel.Destination,
				travel.StartTick),
			_ => throw new InvalidDataException("Fleet travel state is inconsistent."),
		};

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
			StateType = entry.State?.GetType().Name,
			State = entry.State is null
				? JsonSerializer.SerializeToElement<object?>(null, registry.Options)
				: JsonSerializer.SerializeToElement(
					entry.State,
					entry.State.GetType(),
					registry.Options),
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
			nameof(DeliveryObjective) => RestoreDeliveryObjective(dto.Objective, registry.Options),
			nameof(WreckageObjective) => JsonSerializer.Deserialize<WreckageObjective>(
				dto.Objective,
				registry.Options)!,
			_ => throw new InvalidDataException($"Unknown contract objective '{dto.ObjectiveType}'."),
		};
		var contract = new Contract(
				dto.Id, objective, dto.Danger, dto.IssuerFaction, dto.IssuerPoiId,
				dto.Terms, dto.Narrative,
				dto.IsStoryObjective);
		return (
			contract,
			RestoreContractState(dto.State, dto.StateType, contract, registry),
			dto.ExpiresAtTick);
	}

	private static DeliveryObjective RestoreDeliveryObjective(
		JsonElement payload,
		JsonSerializerOptions options)
	{
		if (payload.TryGetProperty("route", out var route))
		{
			var restoredRoute = JsonSerializer.Deserialize<DeliveryRoute>(route, options)
				?? throw new InvalidDataException("Delivery route is missing.");
			var config = payload.TryGetProperty("config", out var configPayload)
				? JsonSerializer.Deserialize<DeliveryGenerationConfig>(configPayload, options)
				: null;
			return new DeliveryObjective(restoredRoute, config);
		}

		return new DeliveryObjective(
			payload.GetProperty("turnInPoiId").GetString()
				?? throw new InvalidDataException("Legacy delivery POI is missing."),
			payload.GetProperty("turnInFacilityId").GetString()
				?? throw new InvalidDataException("Legacy delivery facility is missing."),
			payload.GetProperty("turnInOperatorName").GetString()
				?? throw new InvalidDataException("Legacy delivery operator is missing."));
	}

	private static ContractState? RestoreContractState(
		JsonElement statePayload,
		string? stateType,
		Contract contract,
		PersistenceRegistry registry)
	{
		if (statePayload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
			return null;

		var state = stateType switch
		{
			nameof(DeliveryContractState) => RestoreDeliveryContractState(
				statePayload,
				registry.Options),
			nameof(HuntContractState) => JsonSerializer.Deserialize<HuntContractState>(
				statePayload,
				registry.Options),
			nameof(WreckageContractState) => JsonSerializer.Deserialize<WreckageContractState>(
				statePayload,
				registry.Options),
			_ => JsonSerializer.Deserialize<ContractState>(statePayload, registry.Options),
		};
		if (state is not null && state.GetType() != typeof(ContractState))
			return state;

		if (state is null)
			return null;

		return ContractState.CreateFor(
			contract,
			state.Status,
			state.AcceptedAtTick,
			state.HolderUnitId);
	}

	private static DeliveryContractState RestoreDeliveryContractState(
		JsonElement payload,
		JsonSerializerOptions options)
	{
		var contractId = payload.GetProperty("contractId").GetString()
			?? throw new InvalidDataException("Delivery contract state id is missing.");
		var status = payload.GetProperty("status").Deserialize<EContractStatus>(options);
		var acceptedAtTick = payload.GetProperty("acceptedAtTick").Deserialize<int?>(options);
		var holderUnitId = payload.GetProperty("holderUnitId").GetString();
		var progress = payload.TryGetProperty("progress", out var progressPayload)
			? JsonSerializer.Deserialize<DeliveryProgress>(progressPayload, options)
			: new DeliveryProgress(
				JsonSerializer.Deserialize<IReadOnlyList<bool>>(
					payload.GetProperty("deliveryProgressLegs"),
					options)
					?? throw new InvalidDataException("Delivery progress is missing."));
		return new DeliveryContractState(
			contractId,
			status,
			acceptedAtTick,
			holderUnitId,
			progress ?? throw new InvalidDataException("Delivery progress is missing."));
	}

	private sealed record StarMapStateData(
		string Id, MapUnitType Type, EFaction Faction,
		StarMapTravelData Travel, Coord PatrolOrigin, int PatrolRadius,
		IReadOnlyList<string> ChoreDockIds, int ChoreIndex, double SpeedPerTick,
		double EngageRadius, double VisionRadius,
		Engagement? CurrentEngagement, TravelTarget TravelTarget,
		string PendingWreckContractId,
		int AggressionRating = 0,
		EFleetSpawnerSource SpawnerSource = EFleetSpawnerSource.None,
		int? FleetSpawnerExpiresAtTick = null,
		string? SourceContractId = null,
		FleetPursuitDirective? PursuitDirective = null);

	private sealed record StarMapTravelData(
		Coord? AtRestPosition,
		long JourneyId,
		Coord Origin,
		Coord Destination,
		int StartTick);

	private static HazardSaveDto CaptureNonUnit(NonUnit nonUnit)
	{
		var center = nonUnit switch
		{
			Asteroid asteroid => asteroid.Center,
			Hazard hazard => hazard.Center,
			_ => throw new InvalidOperationException(
				$"Unsupported non-unit type '{nonUnit.GetType().Name}'."),
		};

		return ReflectionJson.Map<HazardSaveDto>(new
		{
			nonUnit.Id,
			nonUnit.ActorId,
			Center = center,
			nonUnit.Frame,
			Cells = nonUnit.Cells.ToArray(),
		});
	}

	private static NonUnit RestoreNonUnit(HazardSaveDto dto) =>
		NonUnitTypeSlug.ParseTypeSlug(dto.Id) switch
		{
			NonUnitTypeSlug.Asteroid => new Asteroid
			{
				Id = dto.Id,
				ActorId = dto.ActorId,
				Center = dto.Center,
				Frame = dto.Frame,
				Cells = dto.Cells.ToFrozenSet(),
			},
			NonUnitTypeSlug.Goop => new GoopHazard
			{
				Id = dto.Id,
				ActorId = dto.ActorId,
				Center = dto.Center,
				Frame = dto.Frame,
				Cells = dto.Cells.ToFrozenSet(),
			},
			var slug => throw new InvalidDataException($"Unsupported non-unit type slug '{slug}'."),
		};

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
			state.Initiative = unitDto.Initiative ?? state.Initiative;
			state.ActionPoints = unitDto.ActionPoints;
			var maneuverability = unitDto.Projectile?.Maneuverability() ?? state.Maneuverability;
			var maxMp = unitDto.MaxMp ?? maneuverability.MaxManeuverPoints;
			state.ManeuverPoints = unitDto.ManeuverPoints ?? maxMp;
			state.FuelRemaining = unitDto.FuelRemaining;
			state.ApPenaltyNextTurn = unitDto.ApPenaltyNextTurn;
			state.Projectile = unitDto.Projectile;
			state.Maneuverability = maneuverability.WithBudgets(
				unitDto.MaxAp,
				maxMp);
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
			RestoreNonUnit);
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

	private static InstalledAbility RestoreInstalledAbility(InstalledAbilityDto dto)
	{
		if (dto.Kind is { } kind)
		{
			return new InstalledAbility(
				kind,
				dto.MountedOn,
				dto.DamageUpgradeTier,
				dto.RangeUpgradeTier);
		}

		if (dto.Spec is null)
			throw new InvalidDataException("Installed ability is missing both kind and legacy spec data.");

		var effective = RestoreAbility(dto.Spec);
		try
		{
			return new InstalledAbility(effective, dto.MountedOn);
		}
		catch (ArgumentException exception)
		{
			throw new InvalidDataException(
				$"Legacy ability '{effective.Kind}' does not match its canonical baseline and upgrades.",
				exception);
		}
	}

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
			EAbilityKind.GoopGun => Deserialize<GoopGunSpec>(dto.Data),
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
