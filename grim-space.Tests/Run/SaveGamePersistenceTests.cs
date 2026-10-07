using System.Text.Json;
using System.Text.Json.Nodes;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Tutorials;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.Factions;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitState = GrimSpace.Battle.Units.State;
using FleetTravel = GrimSpace.World.StarSystem.Units.FleetTravel;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Application;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class SaveGamePersistenceTests
{
	[Fact]
	public void FileSystemStorage_RoundTripsDocument()
	{
		var directory = Directory.CreateTempSubdirectory();
		try
		{
			var storage = new FileSystemSaveGameStorage(
				Path.Combine(directory.FullName, "saves", "save.json"));
			var document = SaveGameDocument.Create(
				"0.1.0",
				"map",
				new { tick = 12, marker = "round-trip" });

			Assert.Equal(SaveStorageResult.Success, storage.TryWrite(document));
			Assert.True(storage.Exists());
			Assert.Equal(SaveStorageResult.Success, storage.TryRead(out var loaded));
			Assert.NotNull(loaded);
			Assert.Equal(document.FormatVersion, loaded.FormatVersion);
			Assert.Equal(document.GameVersion, loaded.GameVersion);
			Assert.Equal("round-trip", loaded.Payload.GetProperty("marker").GetString());
		}
		finally
		{
			directory.Delete(recursive: true);
		}
	}

	[Fact]
	public void FileSystemStorage_TruncatedFileIsCorrupt()
	{
		var directory = Directory.CreateTempSubdirectory();
		try
		{
			var path = Path.Combine(directory.FullName, "save.json");
			File.WriteAllText(path, "{\"formatVersion\":1,\"payload\":");
			var storage = new FileSystemSaveGameStorage(path);

			Assert.Equal(SaveStorageResult.Corrupt, storage.TryRead(out var loaded));
			Assert.Null(loaded);
		}
		finally
		{
			directory.Delete(recursive: true);
		}
	}

	[Fact]
	public void FileSystemStorage_FailedWriteLeavesExistingDocument()
	{
		var directory = Directory.CreateTempSubdirectory();
		try
		{
			var path = Path.Combine(directory.FullName, "save.json");
			var storage = new FileSystemSaveGameStorage(path);
			var original = SaveGameDocument.Create("0.1.0", "map", new { marker = "old" });
			Assert.Equal(SaveStorageResult.Success, storage.TryWrite(original));

			using var blocker = new FileStream(
				$"{path}.tmp",
				FileMode.OpenOrCreate,
				FileAccess.Read,
				FileShare.None);
			var replacement = SaveGameDocument.Create("0.1.0", "map", new { marker = "new" });

			Assert.Equal(SaveStorageResult.Failed, storage.TryWrite(replacement));
			Assert.Equal(SaveStorageResult.Success, storage.TryRead(out var loaded));
			Assert.Equal("old", loaded!.Payload.GetProperty("marker").GetString());
		}
		finally
		{
			directory.Delete(recursive: true);
		}
	}

	[Fact]
	public void SaveLoadResult_RejectsUnknownFormatVersion()
	{
		var document = SaveGameDocument.Create("0.1.0", "map", new { marker = "version" })
			with { FormatVersion = SaveGameDocument.CurrentFormatVersion + 1 };

		Assert.Equal(
			LoadResult.UnsupportedFormat,
			SaveLoadResult.Classify(
				SaveStorageResult.Success,
				document));
	}

	[Fact]
	public void SaveLoadResult_CanApplyExplicitGameVersionPolicy()
	{
		var document = SaveGameDocument.Create("2.0.0", "map", new { marker = "version" });

		Assert.Equal(
			LoadResult.IncompatibleGameVersion,
			SaveLoadResult.Classify(
				SaveStorageResult.Success,
				document,
				isGameVersionCompatible: version => version == "1.0.0"));
		Assert.Equal(
			LoadResult.Success,
			SaveLoadResult.Classify(
				SaveStorageResult.Success,
				document));
	}

	[Fact]
	public void PolicyAllowsStablePlayerInputGateAndBlocksUnsafeStates()
	{
		var stable = new SaveGateState(
			HasActiveRun: true,
			StrategicWaitingForPlayerInput: true,
			StrategicResolvingAction: false,
			StrategicHasUncommittedPlayerBatch: false,
			BattleResolving: false,
			BattleReplaying: false,
			SceneTransitioning: false);
		Assert.Equal(SaveBlockReason.None, SaveLoadPolicy.CanSave(stable));

		Assert.Equal(
			SaveBlockReason.ResolvingBattle,
			SaveLoadPolicy.CanSave(stable with { BattleResolving = true }));
		Assert.Equal(
			SaveBlockReason.UncommittedPlayerBatch,
			SaveLoadPolicy.CanSave(stable with { StrategicHasUncommittedPlayerBatch = true }));
	}

	[Fact]
	public void AutosaveSchedulerSignalsAtIntervalAndRetainsRemainder()
	{
		var scheduler = new AutosaveScheduler(10);

		Assert.False(scheduler.Advance(4));
		Assert.True(scheduler.Advance(6));
		Assert.False(scheduler.Advance(9));
		Assert.True(scheduler.Advance(1));
	}

	[Fact]
	public void AutosaveSchedulerRejectsInvalidTiming()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new AutosaveScheduler(0));
		Assert.Throws<ArgumentOutOfRangeException>(() => new AutosaveScheduler(double.NaN));

		var scheduler = new AutosaveScheduler(10);
		Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Advance(-1));
	}

	[Fact]
	public void RegistryRejectsUnknownDiscriminator()
	{
		var registry = new PersistenceRegistry();
		registry.Register(
			"marker",
			element => element.GetProperty("value").GetString()!,
			value => JsonSerializer.SerializeToElement(new { value }));

		var encoded = registry.Write("value");
		Assert.Equal("value", registry.Read(encoded));
		Assert.Throws<InvalidDataException>(() =>
			registry.Read(JsonSerializer.SerializeToElement(new
			{
				type = "unknown",
				payload = new { },
			})));
	}

	[Fact]
	public void ReflectionJson_RoundTripsDataUsingStableCamelCaseNames()
	{
		var value = new ReflectionSample("sample-id", 7);

		var json = ReflectionJson.Write(value);
		var restored = ReflectionJson.Read<ReflectionSample>(json);

		Assert.Equal(value, restored);
		Assert.Equal(
			["sampleId", "retryCount"],
			json.EnumerateObject().Select(property => property.Name).ToArray());
	}

	[Fact]
	public void ReflectionJson_RoundTripsPolymorphicPayloadWhenConcreteTypeIsProvided()
	{
		object value = new ReflectionSample("sample-id", 7);

		var json = ReflectionJson.Write(value);
		var restored = ReflectionJson.Read(json, value.GetType());

		Assert.Equal(value, Assert.IsType<ReflectionSample>(restored));
	}

	[Fact]
	public void ReflectionJson_MapsMatchingObjectShapeToAnotherType()
	{
		var mapped = ReflectionJson.Map<ReflectionTarget>(
			new ReflectionSample("sample-id", 7));

		Assert.Equal(new ReflectionTarget("sample-id", 7), mapped);
	}

	[Fact]
	public void ReflectionJson_MapsValueTupleMembersToDtoProperties()
	{
		(ResourceId Id, int Balance) balance = (ResourceId.Credits, 42);

		var mapped = ReflectionJson.Map<ResourceBalanceDto>(balance);

		Assert.Equal(ResourceId.Credits, mapped.Id);
		Assert.Equal(42, mapped.Balance);
	}

	[Fact]
	public void DefaultRegistry_RoundTripsBattleActionAndRecord()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var action = new VoidBombAction("ship", ESpatialOrientation.Forward, "void-bomb-1");
		var record = new Record<Transaction>(
			new Transaction("test", ResourceBundle.Of(ResourceId.Credits, 2)));

		Assert.Equal(action, registry.Read(registry.Write(action)));
		Assert.Equal(record, registry.Read(registry.Write(record)));
	}

	[Fact]
	public void DefaultRegistry_RoundTripsStrategicComplexAction()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var path = TransitPath.FromPoints(
			[new Coord(0, 0, 0), new Coord(1, 0, 0)],
			[1d, 1d]);
		var action = new MoveAction("fleet", "fleet", new Coord(1, 0, 0), path);

		var restored = Assert.IsType<MoveAction>(registry.Read(registry.Write(action)));
		Assert.Equal(action.ActorId, restored.ActorId);
		Assert.Equal(action.UnitId, restored.UnitId);
		Assert.Equal(action.Destination, restored.Destination);
		Assert.Equal(action.Path.Legs.Length, restored.Path.Legs.Length);
		Assert.Equal(action.Path.Legs[0].Points.ToArray(), restored.Path.Legs[0].Points.ToArray());
		Assert.Equal(action.Path.Legs[0].SpeedMultiplier, restored.Path.Legs[0].SpeedMultiplier);
		Assert.Equal(action.Path.Legs[0].Length, restored.Path.Legs[0].Length);
	}

	[Fact]
	public void DefaultRegistry_RoundTripsContractActionsWithoutDelegates()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var actions = new IAction[]
		{
			new AcceptContractAction("fleet", "poi", "facility", "operator", "contract", "spawn"),
			new DeclineContractAction("fleet", "poi", "facility", "operator", "contract"),
			new VisitContractMerchantAction("fleet", "poi", "facility", "operator"),
			new CompleteDeliveryFacilityLegAction("fleet", "poi", "facility", "operator", "contract", 0),
			new InvestigateWreckageAction("fleet", "contract", "ambush-spawn"),
		};

		Assert.Collection(
			actions.Select(action => registry.Read(registry.Write(action))),
			restored => Assert.Equal(actions[0], restored),
			restored => Assert.Equal(actions[1], restored),
			restored => Assert.Equal(actions[2], restored),
			restored => Assert.Equal(actions[3], restored),
			restored => Assert.Equal(actions[4], restored));
	}

	[Fact]
	public void DefaultRegistry_RoundTripsFleetSpawnerAction()
	{
		var map = StarMap.Create(42);
		var fleet = map.FleetRegistry.All.First();
		var action = new MaintainFleetSpawnerAction(
			"fleet-spawner",
			0,
			[new FleetSpawnerAddition(fleet)]);
		var registry = PersistenceRegistry.CreateDefault();

		var restored = Assert.IsType<MaintainFleetSpawnerAction>(
			registry.Read(registry.Write(action)));

		var restoredFleet = Assert.Single(restored.Additions).Fleet;
		Assert.Equal(fleet.State.Id, restoredFleet.State.Id);
		Assert.Equal(fleet.Members.Select(member => member.Id), restoredFleet.Members.Select(member => member.Id));
	}

	[Fact]
	public void DefaultRegistry_RoundTripsResolveEngagementAction()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var action = new ResolveEngagementAction(
			"fleet",
			new BattleOutcome("battle", EBattleResult.Win, []),
			[new LootRoll("unit", EType.Fighter, ResourceBundle.Of(ResourceId.Credits, 3))],
			ResourceBundle.Of(ResourceId.Credits, 3));

		var restored = Assert.IsType<ResolveEngagementAction>(
			registry.Read(registry.Write(action)));
		Assert.Equal(action.InitiatorId, restored.InitiatorId);
		Assert.Equal(action.Outcome.BattleId, restored.Outcome.BattleId);
		Assert.Equal(action.Outcome.Result, restored.Outcome.Result);
		Assert.Empty(restored.Outcome.StateHandoffs);
		Assert.Equal(action.LootRolls, restored.LootRolls);
		Assert.Equal(action.LootTotal, restored.LootTotal);
	}

	[Fact]
	public void DefaultRegistry_RoundTripsSpawnFactsAsStateDto()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var ship = ShipCatalog.CreateInstance("miner", EType.RepurposedMiner);
		var state = BattleUnitState.FromShipInstance(
			ship,
			new Coord(2, 3, 4),
			Coord.Forward,
			Coord.Up,
			"parent");
		state.ActionPoints = 4;
		state.FuelRemaining = 2;
		var record = new Record<SpawnFacts>(
			new SpawnFacts("source", "miner", EType.RepurposedMiner, state));

		var restored = Assert.IsType<Record<SpawnFacts>>(
			registry.Read(registry.Write(record)));
		Assert.Equal(record.Value.SourceId, restored.Value.SourceId);
		Assert.Equal(record.Value.TargetId, restored.Value.TargetId);
		Assert.Equal(record.Value.EntityType, restored.Value.EntityType);
		Assert.Equal(state.Id, restored.Value.SpawnedState.Id);
		Assert.Equal(state.Type, restored.Value.SpawnedState.Type);
		Assert.Equal(state.Position, restored.Value.SpawnedState.Position);
		Assert.Equal(state.Loadout.MaxHullPoints, restored.Value.SpawnedState.Loadout.MaxHullPoints);
		Assert.Equal(
			state.Loadout.InstalledAbilities.Count,
			restored.Value.SpawnedState.Loadout.InstalledAbilities.Count);
		Assert.Equal(state.HullPoints, restored.Value.SpawnedState.HullPoints);
		Assert.Equal(state.ActionPoints, restored.Value.SpawnedState.ActionPoints);
		Assert.Equal(state.FuelRemaining, restored.Value.SpawnedState.FuelRemaining);
		Assert.Equal(state.ParentId, restored.Value.SpawnedState.ParentId);
	}

	[Fact]
	public void SaveDtoMapper_RoundTripsRegisteredTimelineEntries()
	{
		var registry = new PersistenceRegistry();
		registry.Register(
			"heading_turn",
			element => element.Deserialize<HeadingTurnAction>()!,
			value => JsonSerializer.SerializeToElement(value));

		var timeline = new Timeline();
		timeline.Clock.Set(4);
		var history = new HeadingTurnAction("a", EHeadingTurn.YawRight);
		var pending = new HeadingTurnAction("b", EHeadingTurn.YawLeft);
		timeline.Append(history);
		timeline.Schedule(2, pending);

		var dto = SaveDtoMapper.CaptureTimeline(timeline.ToSnapshot(), registry);
		var json = JsonSerializer.Serialize(dto);
		var restoredDto = JsonSerializer.Deserialize<TimelineSnapshotDto>(json)!;
		var restored = Timeline.From(SaveDtoMapper.RestoreTimeline(restoredDto, registry));

		Assert.Equal(4, restored.Clock.Current);
		Assert.Equal(history, Assert.Single(restored.History(4)));
		Assert.Equal(pending, Assert.Single(restored.TakePending(6)));
	}

	[Fact]
	public void PersistenceRegistry_RoundTripsContractBoardActionWithoutDelegateSerialization()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var contract = new Contract(
			"contract-1",
			new DeliveryObjective("poi-1", "facility-1", "operator"),
			EDangerLevel.Low,
			EFaction.Player,
			"issuer-poi",
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 10)),
			new ContractNarrative("Delivery", "Deliver the cargo."));
		var action = new MaintainContractBoardAction(
			"contracts",
			4,
			[new ContractAddition(contract, 24)]);

		var restored = Assert.IsType<MaintainContractBoardAction>(
			registry.Read(registry.Write(action)));

		var addition = Assert.Single(restored.Additions);
		Assert.Equal(action.ActorId, restored.ActorId);
		Assert.Equal(action.Tick, restored.Tick);
		Assert.Equal(contract.Id, addition.Contract.Id);
		Assert.Equal(24, addition.ExpiresAtTick);
		Assert.Equal(
			typeof(DeliveryObjective),
			addition.Contract.Objective.GetType());
		Assert.NotNull(addition.Contract.Objective);
	}

	[Fact]
	public void PersistenceRegistry_RoundTripsMultiLegDeliveryObjective()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var objective = new DeliveryObjective(
			new DeliveryRoute(
			[
				new FacilityDeliveryLeg("poi-a", "facility-a", "operator-a"),
				new FacilityDeliveryLeg("poi-b", "facility-b", "operator-b"),
			]));
		var contract = new Contract(
			"contract-route",
			objective,
			EDangerLevel.Low,
			EFaction.Player,
			"issuer-poi",
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 10)),
			new ContractNarrative("Delivery", "Deliver the cargo."));
		var action = new MaintainContractBoardAction(
			"contracts",
			4,
			[new ContractAddition(contract, 24)]);

		var restored = Assert.IsType<MaintainContractBoardAction>(
			registry.Read(registry.Write(action)));
		var restoredObjective = Assert.IsType<DeliveryObjective>(
			Assert.Single(restored.Additions).Contract.Objective);

		Assert.Equal(objective.Route.Legs.Select(leg => leg.ToString()), restoredObjective.Route.Legs.Select(leg => leg.ToString()));
		Assert.Equal(2, restoredObjective.RouteLegCount);
	}

	[Fact]
	public void SaveDtoMapper_RoundTripsIdentityStateAndLoadout()
	{
		var ship = ShipCatalog.CreateInstance("fighter-1", EType.Fighter);
		Assert.True(ship.TryWithUpgradedMaxHull(out ship));
		Assert.True(ship.TryWithUpgradedMaxShields(
			ESpatialOrientation.Forward,
			out ship));
		ship.HullPoints = 3;

		var restored = SaveDtoMapper.RestoreShip(SaveDtoMapper.CaptureShip(ship));

		Assert.Equal(ship.Id, restored.Id);
		Assert.Equal(ship.Spec.Chassis, restored.Spec.Chassis);
		Assert.Equal(ship.HullPoints, restored.HullPoints);
		Assert.True(ship.ShieldPoints.Matches(restored.ShieldPoints));
		Assert.Equal(ship.Loadout.MaxHullPoints, restored.Loadout.MaxHullPoints);
		Assert.True(
			ship.Loadout.MaxShieldPoints.Matches(restored.Loadout.MaxShieldPoints));
		Assert.True(
			ship.Loadout.ShieldUpgradeTiers.Matches(restored.Loadout.ShieldUpgradeTiers));
		Assert.Equal(ship.Loadout.HullUpgradeTier, restored.Loadout.HullUpgradeTier);
		Assert.Equal(ship.Loadout.InstalledAbilities, restored.Loadout.InstalledAbilities);
	}

	[Fact]
	public void SaveDtoMapper_RoundTripsPlayerResources()
	{
		var map = StarMap.Create(42);
		map.PlayerResources.TryApply(ResourceBundle.Create(
			(ResourceId.Credits, 120),
			(ResourceId.ScrapAlloy, 7)));
		var registry = PersistenceRegistry.CreateDefault();

		var restored = SaveDtoMapper.RestoreStarMap(
			SaveDtoMapper.CaptureStarMap(map, registry),
			registry);

		Assert.Equal(120, restored.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(7, restored.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void SaveDtoMapper_RoundTripsContractIssuerCooldowns()
	{
		var map = StarMap.Create(42);
		var poiId = map.Blueprint.SupplyPlan.AdministrativePoiId;
		map.ContractRegistry.PauseIssuerGeneration(poiId, untilTick: 151);
		var registry = PersistenceRegistry.CreateDefault();

		var restored = SaveDtoMapper.RestoreStarMap(
			SaveDtoMapper.CaptureStarMap(map, registry),
			registry);

		Assert.True(restored.ContractRegistry.IsIssuerGenerationCoolingDown(poiId, currentTick: 150));
		Assert.False(restored.ContractRegistry.IsIssuerGenerationCoolingDown(poiId, currentTick: 151));
	}

	[Fact]
	public void SaveDtoMapper_RoundTripsConcretePoiAndFacilityIdentity()
	{
		var map = StarMap.Create(42);
		var registry = PersistenceRegistry.CreateDefault();

		var dto = SaveDtoMapper.CaptureStarMap(map, registry);
		var restored = SaveDtoMapper.RestoreStarMap(dto, registry);

		Assert.Equal(
			map.PointsOfInterest.Select(poi => poi.GetType()),
			restored.PointsOfInterest.Select(poi => poi.GetType()));
		Assert.Equal(
			map.PointsOfInterest.Select(poi => poi.Id),
			restored.PointsOfInterest.Select(poi => poi.Id));

		foreach (var original in map.PointsOfInterest)
		{
			var copy = restored.PointsOfInterest.Single(poi => poi.Id == original.Id);
			Assert.Equal(
				original.Facilities.Select(facility => facility.Id),
				copy.Facilities.Select(facility => facility.Id));
			Assert.Equal(
				original.Facilities
					.SelectMany(facility => facility.Operators)
					.Select(operatorEntry => operatorEntry.Name),
				copy.Facilities
					.SelectMany(facility => facility.Operators)
					.Select(operatorEntry => operatorEntry.Name));
		}

		var originalAdmin = Assert.IsType<AdministrativeCore>(
			map.PointsOfInterest.Single(poi => poi is AdministrativeCore));
		var restoredAdmin = Assert.IsType<AdministrativeCore>(
			restored.PointsOfInterest.Single(poi => poi is AdministrativeCore));
		Assert.Equal(originalAdmin.PhysicalForm, restoredAdmin.PhysicalForm);
	}

	[Fact]
	public void SaveDtoMapper_RestoresLegacyCombatProfileAndFleetParticipants()
	{
		var map = StarMap.Create(42);
		const string playerId = "save-test-player";
		StarSystemTestHarness.AddPlayerFleet(map, playerId);
		map.FleetRegistry.Add(
			StarSystemTestHarness.CreatePirateFleet(
				"save-test-pirate",
				new Coord(20, 0, 20),
				EFaction.Pirates));
		var registry = PersistenceRegistry.CreateDefault();
		var captured = SaveDtoMapper.CaptureStarMap(map, registry);
		var legacyState = JsonNode.Parse(captured.Fleets[0].State.GetRawText())!.AsObject();
		legacyState["combatProfile"] = new JsonObject();
		var legacyFleet = captured.Fleets[0] with
		{
			State = JsonSerializer.SerializeToElement(legacyState, registry.Options),
		};
		var legacySave = captured with
		{
			Fleets = captured.Fleets
				.Select((fleet, index) => index == 0 ? legacyFleet : fleet)
				.ToArray(),
		};

		var restored = SaveDtoMapper.RestoreStarMap(legacySave, registry);
		var player = restored.FleetRegistry.FleetOf(playerId);
		var targetIds = restored.FleetRegistry.All
			.Where(fleet => fleet.State.Id != playerId)
			.Select(fleet => fleet.State.Id)
			.ToArray();

		Assert.Contains(
			restored.FleetRegistry.All,
			fleet => fleet.State.Faction == EFaction.Pirates);
		Assert.Contains(
			restored.FleetRegistry.All,
			fleet => fleet.State.Faction == EFaction.TheOptimality);
		foreach (var targetId in targetIds)
		{
			var target = restored.FleetRegistry.FleetOf(targetId);
			var action = new PursueContactAction(
				playerId,
				new FleetContactTarget(targetId),
				Assert.IsType<FleetTravel.AtRest>(target.State.Travel).Position,
				TransitPath.FromPoints(
					[
						Assert.IsType<FleetTravel.AtRest>(player.State.Travel).Position,
						Assert.IsType<FleetTravel.AtRest>(target.State.Travel).Position,
					],
					[1.0, 1.0]),
				EContactIntent.Engagement);

			Assert.True(PursueContactDef.Instance.IsLegal(
				action,
				restored,
				new GrimSpace.World.StarSystem.Runtime.ActorRuntime()));
		}
	}

	[Fact]
	public void SaveDtoMapper_PreservesTutorialHuntChassisAfterMapRestore()
	{
		var map = StarMap.Create(42);
		var contractId = TutorialBeatContracts.OfferBeatA(map)!;
		var registry = PersistenceRegistry.CreateDefault();
		Assert.True(map.ContractRegistry.TryGet(contractId, out var originalContract));
		var original = Assert.IsType<HuntObjective>(
			originalContract.Objective);

		var restored = SaveDtoMapper.RestoreStarMap(
			SaveDtoMapper.CaptureStarMap(map, registry),
			registry);
		Assert.True(restored.ContractRegistry.TryGet(contractId, out var restoredContract));
		var restoredObjective = Assert.IsType<HuntObjective>(
			restoredContract.Objective);

		Assert.Equal(
			original.SpawnGroups[0].Spawn.Members,
			restoredObjective.SpawnGroups[0].Spawn.Members);
		Assert.All(
			restoredObjective.SpawnGroups[0].Spawn.Members,
			member => Assert.NotEqual(EType.Fighter, member.Chassis));

		var spawned = ContractEnemySpawner.SpawnForContract(
			restoredContract,
			restored,
			"test-member");
		Assert.All(
			spawned.Fleets.SelectMany(fleet => fleet.Registrations),
			registration => Assert.NotEqual(EType.Fighter, registration.Chassis));
	}

	[Fact]
	public void SaveDtoMapper_RoundTripsBattleWorldState()
	{
		using var orchestrator = BattleOrchestrator.FromEncounter(
			BattleEncounter.DevDefault(seed: 22, gridSize: 12),
			gridSize: 12);
		var registry = new PersistenceRegistry();
		var dto = SaveDtoMapper.CaptureBattleWorld(orchestrator.Engine.World, registry);
		var restored = SaveDtoMapper.RestoreBattleWorld(dto, registry);

		Assert.Equal(dto.BattleId, restored.BattleId);
		Assert.Equal(dto.Objective, restored.Objective);
		Assert.Equal(dto.GridWidth, restored.Grid.Width);
		Assert.Equal(dto.Units.Count, restored.UnitRegistry.All.Count());
		Assert.Equal(dto.Hazards.Count, restored.Hazards.Count());
		Assert.Equal(
			dto.EngagedShipIds.OrderBy(id => id),
			restored.EngagedShipIds.OrderBy(id => id));
		var playerId = dto.Units
			.Single(unit => unit.Team == ETeam.Player)
			.Ship.Id;
		using var restoredOrchestrator =
			BattleOrchestrator.FromSavedWorld(restored, playerId);
		Assert.All(restoredOrchestrator.Engine.World.UnitRegistry.All, unit =>
			Assert.True(unit.ExecutionAgent.IsInitialized));
	}

	private sealed record ReflectionSample(
		string SampleId,
		int RetryCount);

	private sealed record ReflectionTarget(
		string SampleId,
		int RetryCount);
}
