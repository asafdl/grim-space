using System.Collections.Frozen;
using System.Text.Json;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Pilots;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using BattleUnitState = GrimSpace.Battle.Units.State;
using GrimSpace.Run.Items;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
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
using GrimSpace.World.StarSystem.Merchants;
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
			runtime.SelectedMemberShipId,
			runtime.FuelLedgerJourneyId,
			runtime.FuelChargedDistanceAlongCurrentJourney,
			runtime.FuelUnchargedDistance);
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
			?? throw new InvalidDataException("Saved runtime completion is not an action.")
			: null;
		runtime.PendingCompletionTick = dto.PendingCompletionTick;
		runtime.JourneyIdSequence = dto.JourneyIdSequence;
		runtime.ActionCooldownUntilTick = dto.ActionCooldownUntilTick;
		runtime.SelectedMemberShipId = dto.SelectedMemberShipId;
		runtime.ApplyTravelFuelLedger(
			dto.FuelLedgerJourneyId,
			dto.FuelChargedDistanceAlongCurrentJourney,
			dto.FuelUnchargedDistance);
		runtime.IgnoreUntilTickByTargetId.Clear();
		foreach (var (targetId, ignoreUntilTick) in dto.IgnoreUntilTickByTargetId)
			runtime.IgnoreUntilTickByTargetId[targetId] = ignoreUntilTick;
	}

	public static BattleEncounterSaveDto CaptureBattleEncounter(BattleEncounter encounter)
	{
		ArgumentNullException.ThrowIfNull(encounter);
		return ReflectionJson.Map<BattleEncounterSaveDto>(new
		{
			encounter.Seed,
			encounter.Id,
			encounter.Objective,
			encounter.XpDifficulty,
			Spawns = encounter.Spawns.Select(spawn =>
				ReflectionJson.Map<BattleEncounterSpawnDto>(new
				{
					Ship = CaptureShip(spawn.Ship),
					Pilot = spawn.Pilot is null
						? null
						: new PilotBattleSnapshotDto(
							PilotId: spawn.Pilot.PilotId,
							CapabilityGrants: spawn.Pilot.CapabilityGrants,
							PortraitId: spawn.Pilot.PortraitId),
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
			XpDifficulty = dto.XpDifficulty,
			Spawns = dto.Spawns.Select(RestoreBattleSpawn).ToArray(),
			WorldHazards = dto.Hazards.Select(hazard => new BattleHazardSpawn
			{
				Origin = hazard.Origin,
				Cells = hazard.Cells.ToHashSet(),
			}).ToArray(),
		};
	}

	private static BattleSpawn RestoreBattleSpawn(BattleEncounterSpawnDto spawn)
	{
		PilotBattleSnapshot? pilot = null;
		if (spawn.Pilot is { } snapshot)
		{
			pilot = new PilotBattleSnapshot(
				snapshot.PilotId,
				snapshot.CapabilityGrants.ToArray(),
				snapshot.PortraitId);
		}

		return new BattleSpawn
		{
			Ship = RestoreShip(spawn.Ship),
			Pilot = pilot,
			Team = spawn.Team,
			Position = spawn.Position,
			Fore = spawn.Fore,
			Dorsal = spawn.Dorsal,
			ExecutionAgent = BattleAgentFactory.Create(spawn.AgentKind),
		};
	}

	public static ShipPersistenceDto CaptureShip(ShipInstance ship, string? ownerId = null)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var loadout = ship.Loadout;
		return ReflectionJson.Map<ShipPersistenceDto>(new
		{
			ship.Id,
			Chassis = ship.Spec.Chassis,
			ship.CurrentHullPoints,
			CurrentShieldPoints = CaptureFaces(ship.CurrentShieldPoints),
			BaseHullPoints = loadout.BaseHullPoints,
			BaseShieldPoints = CaptureFaces(loadout.BaseShieldPoints),
			ShieldUpgradeTiers = CaptureFaces(loadout.ShieldUpgradeTiers),
			loadout.HullUpgradeTier,
			InstalledAbilities = loadout.InstalledAbilities
				.Select(installed => new InstalledAbilityDto(
					installed.MountedOn,
					installed.Kind,
					installed.DamageUpgradeTier,
					installed.RangeUpgradeTier))
				.ToArray(),
			OwnerId = ownerId,
			ChassisBuffs = loadout.Buffs.ToArray(),
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
			dto.BaseHullPoints,
			RestoreFaces(dto.BaseShieldPoints),
			installed,
			RestoreFaces(dto.ShieldUpgradeTiers),
			dto.HullUpgradeTier,
			dto.ChassisBuffs);
		return new ShipInstance(
			dto.Id,
			spec,
			loadout,
			dto.CurrentHullPoints,
			RestoreFaces(dto.CurrentShieldPoints));
	}

	public static BattleUnitStateDto CaptureBattleUnitState(BattleUnitState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var ship = CaptureShip(new ShipInstance(
				state.Id,
				ShipCatalog.SpecFor(state.Type),
				state.Loadout.DeepCopy(),
				state.CurrentHullPoints,
				state.CurrentShieldPoints.Clone()));
		return ReflectionJson.Map<BattleUnitStateDto>(new
		{
			Ship = ship,
			state.Initiative,
			state.Position,
			state.Fore,
			state.Dorsal,
			state.Starboard,
			state.CurrentActionPoints,
			state.CurrentManeuverPoints,
			state.FuelRemaining,
			state.ParentId,
			Statuses = state.Statuses.Select(CaptureStatus).ToArray(),
			state.ShieldsUpUsedThisTurn,
			state.OverchargeEnginesCooldownRemaining,
			state.MarkWeakspotCooldownRemaining,
			state.ShieldBraceUsedThisTurn,
			state.HeatVentsUsedThisTurn,
			state.DefensiveManeuversMpUsed,
			state.ShieldDynamoGrantedPerFace,
			state.ThirstForScrapStacks,
			state.ReactiveShieldsStacks,
			state.Heat,
			state.Overdrive,
			state.Overheat,
			BaseAp = state.Maneuverability.BaseActionPoints,
			BaseMp = state.Maneuverability.BaseManeuverPoints,
			HeatCapacity = state.HeatProfile.Capacity,
			HeatClearPerTurn = state.HeatProfile.ClearPerTurn,
			HeatGainMultiplier = state.HeatProfile.GainMultiplier,
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
		state.Initiative = dto.Initiative;
		state.CurrentActionPoints = dto.CurrentActionPoints;
		var maneuverability = dto.Projectile?.Maneuverability() ?? state.Maneuverability;
		var maxMp = dto.BaseMp;
		state.CurrentManeuverPoints = dto.CurrentManeuverPoints;
		state.FuelRemaining = dto.FuelRemaining;
		RestoreStatuses(state, dto.Statuses);
		state.ShieldsUpUsedThisTurn = dto.ShieldsUpUsedThisTurn;
		state.OverchargeEnginesCooldownRemaining = dto.OverchargeEnginesCooldownRemaining;
		state.MarkWeakspotCooldownRemaining = dto.MarkWeakspotCooldownRemaining;
		state.ShieldBraceUsedThisTurn = dto.ShieldBraceUsedThisTurn;
		state.HeatVentsUsedThisTurn = dto.HeatVentsUsedThisTurn;
		state.DefensiveManeuversMpUsed = dto.DefensiveManeuversMpUsed;
		state.ShieldDynamoGrantedPerFace = dto.ShieldDynamoGrantedPerFace;
		state.ThirstForScrapStacks = dto.ThirstForScrapStacks;
		state.ReactiveShieldsStacks = dto.ReactiveShieldsStacks;
		RestoreHeat(state, dto.Heat, dto.HeatCapacity, dto.HeatClearPerTurn, dto.HeatGainMultiplier, dto.Overdrive, dto.Overheat);
		state.Projectile = dto.Projectile;
		state.Maneuverability = maneuverability.WithBudgets(
			dto.BaseAp,
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
				state.CurrentHullPoints,
				state.CurrentShieldPoints.Clone());
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
				state.CurrentActionPoints,
				state.CurrentManeuverPoints,
				state.FuelRemaining,
				state.ParentId,
				Statuses = state.Statuses.Select(CaptureStatus).ToArray(),
				state.ShieldsUpUsedThisTurn,
				state.OverchargeEnginesCooldownRemaining,
				state.MarkWeakspotCooldownRemaining,
				state.ShieldBraceUsedThisTurn,
				state.HeatVentsUsedThisTurn,
				state.DefensiveManeuversMpUsed,
				state.ShieldDynamoGrantedPerFace,
				state.ThirstForScrapStacks,
				state.ReactiveShieldsStacks,
				state.Heat,
				state.Overdrive,
				state.Overheat,
				BaseAp = state.Maneuverability.BaseActionPoints,
				BaseMp = state.Maneuverability.BaseManeuverPoints,
				HeatCapacity = state.HeatProfile.Capacity,
				HeatClearPerTurn = state.HeatProfile.ClearPerTurn,
				HeatGainMultiplier = state.HeatProfile.GainMultiplier,
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
			StoryObjectives = map.StoryObjectives.Active,
			Resources = map.PlayerResources.EnumerateBalances()
				.Select(pair => ReflectionJson.Map<ResourceBalanceDto>(pair, registry.Options))
				.ToArray(),
			map.WaitingForPlayerInput,
			map.ActiveNarrativeId,
			Timeline = CaptureTimeline(map.Timeline.ToSnapshot(), registry),
			Market = CaptureMarket(map.Market),
		}, registry.Options);
	}

	public static StarMap RestoreStarMap(
		StarMapSaveDto dto,
		PersistenceRegistry registry,
		Inventory? itemInventory = null)
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
				new FacilityOperatorTemporaryRoles.SnapshotEntry(
					role.FacilityId,
					role.OperatorName,
					role.Role,
					role.MerchantSubtype,
					role.SourceId,
					role.Priority,
					role.AcceptsSourcesUntilTick)));
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
			fleets.Add(RestoreFleet(saved, registry));
		var contracts = new ContractRegistry();
		contracts.RestoreSnapshot(
			dto.Contracts.Select(contract => RestoreContract(contract, registry)).Select(entry =>
				(entry.Contract, entry.State, entry.ExpiresAtTick)),
			dto.MaxPendingContracts);
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
			RestoreMarket(dto.Market),
			terrain,
			itemInventory,
			waitingForPlayerInput: dto.WaitingForPlayerInput,
			activeNarrativeId: dto.ActiveNarrativeId);
	}

	private static StarMarketDto CaptureMarket(StarMarket market) =>
		new(
			market.MerchantListings.Select(listing => new MerchantListingDto(
				listing.Id,
				listing.Catalog,
				listing.Offering?.Kind,
				listing.Pricing.Kind,
				listing.Pricing.FixedCost,
				listing.Pricing.ShieldRechargeCreditsPerPoint)).ToArray(),
			market.RecruitmentListings.Select(listing => new RecruitmentListingDto(
				listing.Id,
				listing.Chassis,
				listing.GearTier,
				listing.Cost)).ToArray());

	private static StarMarket RestoreMarket(StarMarketDto dto)
	{
		var merchantListings = dto.MerchantListings.Select(saved =>
		{
			MerchantCatalog.Offering? offering = saved.OfferingKind is { } kind
				? new MerchantCatalog.Offering(kind)
				: null;
			var pricing = saved.PricingKind switch
			{
				MerchantListingPricingKind.FixedBundle =>
					MerchantListingPricing.Fixed(saved.FixedCost),
				MerchantListingPricingKind.ShieldRechargePerPoint =>
					MerchantListingPricing.ShieldRecharge(saved.ShieldRechargeCreditsPerPoint),
				_ => throw new InvalidDataException(
					$"Unknown merchant listing pricing '{saved.PricingKind}'."),
			};
			return new MerchantListing(
				saved.Id,
				saved.Catalog,
				offering,
				pricing);
		}).ToArray();

		var recruitmentListings = dto.RecruitmentListings.Select(saved =>
			new RecruitmentListing(
				saved.Id,
				saved.Chassis,
				saved.GearTier,
				saved.Cost)).ToArray();

		return new StarMarket(merchantListings, recruitmentListings);
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

	private static Fleet RestoreFleet(StarMapFleetDto dto, PersistenceRegistry registry)
	{
		var state = JsonSerializer.Deserialize<StarMapStateData>(dto.State, registry.Options)
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
			entry.Contract.Terms,
			entry.Contract.Narrative,
			entry.Contract.IsStoryObjective,
			entry.Contract.Category,
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
			nameof(BlackMarketSaleObjective) => JsonSerializer.Deserialize<BlackMarketSaleObjective>(
				dto.Objective,
				registry.Options)!,
			_ => throw new InvalidDataException($"Unknown contract objective '{dto.ObjectiveType}'."),
		};
		var contract = new Contract(
				dto.Id, objective, dto.Danger, dto.IssuerFaction,
				dto.Terms, dto.Narrative,
				dto.IsStoryObjective,
				dto.Category);
		return (
			contract,
			RestoreContractState(dto.State, dto.StateType, registry),
			dto.ExpiresAtTick);
	}

	private static DeliveryObjective RestoreDeliveryObjective(
		JsonElement payload,
		JsonSerializerOptions options)
	{
		var pickupPoiId = payload.GetProperty("pickupPoiId").GetString()
			?? throw new InvalidDataException("Delivery pickup POI is missing.");
		var route = JsonSerializer.Deserialize<DeliveryRoute>(
			payload.GetProperty("route"),
			options)
			?? throw new InvalidDataException("Delivery route is missing.");
		var config = JsonSerializer.Deserialize<DeliveryGenerationConfig>(
			payload.GetProperty("config"),
			options)
			?? throw new InvalidDataException("Delivery generation config is missing.");
		return new DeliveryObjective(pickupPoiId, route, config);
	}

	private static ContractState? RestoreContractState(
		JsonElement statePayload,
		string? stateType,
		PersistenceRegistry registry)
	{
		if (statePayload.ValueKind == JsonValueKind.Null && stateType is null)
			return null;
		if (statePayload.ValueKind != JsonValueKind.Object || stateType is null)
			throw new InvalidDataException("Saved contract state is malformed.");

		return stateType switch
		{
			nameof(DeliveryContractState) => RestoreDeliveryContractState(
				statePayload,
				registry.Options),
			nameof(HuntContractState) => JsonSerializer.Deserialize<HuntContractState>(
					statePayload,
					registry.Options)
				?? throw new InvalidDataException("Saved hunt contract state is missing."),
			nameof(WreckageContractState) => JsonSerializer.Deserialize<WreckageContractState>(
					statePayload,
					registry.Options)
				?? throw new InvalidDataException("Saved wreckage contract state is missing."),
			nameof(LeadContractState) => JsonSerializer.Deserialize<LeadContractState>(
					statePayload,
					registry.Options)
				?? throw new InvalidDataException("Saved lead contract state is missing."),
			nameof(ContractState) => JsonSerializer.Deserialize<ContractState>(
					statePayload,
					registry.Options)
				?? throw new InvalidDataException("Saved contract state is missing."),
			_ => throw new InvalidDataException($"Unknown contract state type '{stateType}'."),
		};
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
		var progress = JsonSerializer.Deserialize<DeliveryProgress>(
			payload.GetProperty("progress"),
			options)
			?? throw new InvalidDataException("Delivery progress is missing.");
		return new DeliveryContractState(
			contractId,
			status,
			acceptedAtTick,
			holderUnitId,
			progress);
	}

	private sealed record StarMapStateData(
		string Id, MapUnitType Type, EFaction Faction,
		StarMapTravelData Travel, Coord PatrolOrigin, int PatrolRadius,
		IReadOnlyList<string> ChoreDockIds, int ChoreIndex, double SpeedPerTick,
		double EngageRadius, double VisionRadius,
		Engagement? CurrentEngagement, TravelTarget TravelTarget,
		string PendingWreckContractId,
		int AggressionRating,
		EFleetSpawnerSource SpawnerSource,
		int? FleetSpawnerExpiresAtTick,
		string? SourceContractId,
		FleetPursuitDirective? PursuitDirective);

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
			state.Initiative = unitDto.Initiative;
			state.CurrentActionPoints = unitDto.CurrentActionPoints;
			var maneuverability = unitDto.Projectile?.Maneuverability() ?? state.Maneuverability;
			var maxMp = unitDto.BaseMp;
			state.CurrentManeuverPoints = unitDto.CurrentManeuverPoints;
			state.FuelRemaining = unitDto.FuelRemaining;
			RestoreStatuses(state, unitDto.Statuses);
			state.ShieldsUpUsedThisTurn = unitDto.ShieldsUpUsedThisTurn;
			state.OverchargeEnginesCooldownRemaining = unitDto.OverchargeEnginesCooldownRemaining;
			state.MarkWeakspotCooldownRemaining = unitDto.MarkWeakspotCooldownRemaining;
			state.ShieldBraceUsedThisTurn = unitDto.ShieldBraceUsedThisTurn;
			state.HeatVentsUsedThisTurn = unitDto.HeatVentsUsedThisTurn;
			state.DefensiveManeuversMpUsed = unitDto.DefensiveManeuversMpUsed;
			state.ShieldDynamoGrantedPerFace = unitDto.ShieldDynamoGrantedPerFace;
			state.ThirstForScrapStacks = unitDto.ThirstForScrapStacks;
			state.ReactiveShieldsStacks = unitDto.ReactiveShieldsStacks;
			RestoreHeat(
				state,
				unitDto.Heat,
				unitDto.HeatCapacity,
				unitDto.HeatClearPerTurn,
				unitDto.HeatGainMultiplier,
				unitDto.Overdrive,
				unitDto.Overheat);
			state.Projectile = unitDto.Projectile;
			state.Maneuverability = maneuverability.WithBudgets(
				unitDto.BaseAp,
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

	private static InstalledAbility RestoreInstalledAbility(InstalledAbilityDto dto) =>
		new(
			dto.Kind,
			dto.MountedOn,
			dto.DamageUpgradeTier,
			dto.RangeUpgradeTier);

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

	private static BattleStatusDto CaptureStatus(BattleStatus status) =>
		BattleStatusJsonConverter.ToDto(status);

	private static void RestoreStatuses(BattleUnitState state, IReadOnlyList<BattleStatusDto> dtos)
	{
		if (dtos is null)
			throw new InvalidDataException("Saved battle statuses are missing.");
		state.Statuses.Clear();
		foreach (var dto in dtos)
		{
			state.Statuses.Add(
				BattleStatusRehydrator.Rehydrate(BattleStatusJsonConverter.ShellFromDto(dto)));
		}
	}

	private static void RestoreHeat(
		BattleUnitState state,
		int heat,
		int heatCapacity,
		int heatClearPerTurn,
		float heatGainMultiplier,
		bool overdrive,
		bool overheat)
	{
		state.Heat = heat;
		state.Overdrive = overdrive;
		state.Overheat = overheat;
		state.HeatProfile = new HeatSpec(heatCapacity, heatClearPerTurn, heatGainMultiplier);

		BattleStatuses.SyncOverdrive(state);
		BattleStatuses.SyncOverheat(state);
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

}
