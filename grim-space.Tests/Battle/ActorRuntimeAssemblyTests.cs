using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Tests.BattleRuntime;

[BattleTestSuite]
public sealed class ActorRuntimeAssemblyTests
{
	[Fact]
	public void From_EmptyPilot_HasNoModifiers()
	{
		var runtime = ActorRuntime.From(null);

		Assert.Null(runtime.PilotId);
		Assert.Empty(runtime.PilotCapabilities);
	}

	[Fact]
	public void From_IonizedSnapshot_AppliesShieldBonusThroughStatusPass()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("player", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills(
				"pilot-1",
				[EPilotSkillId.IonizedPayload]),
		};
		var battle = BattleTestFixture.BeginSimulation(player, enemy, pilotsByActorId: pilots);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		var action = new LightningCannonAction(player.State.Id);
		var effects = BattleStatuses.RewriteOutgoing(
			action,
			battle.Engine.World,
			LightningCannonDef.Instance.Resolve(action, battle.Engine.World, runtime));

		var damage = Assert.IsType<GrimSpace.Battle.Effects.ApplyChainLightningDamageEffect>(
			effects.First(effect => effect is GrimSpace.Battle.Effects.ApplyChainLightningDamageEffect));
		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			damage.ShieldDamageBonusPercent);
		Assert.Contains(
			battle.Engine.World.StateOf(player.State.Id).Statuses,
			status => status.InstanceId
				== BattleStatuses.PassiveInstanceId(
					nameof(EPilotCapabilityId.LightningShieldDamageBonus),
					player.State.Id));
	}

	[Fact]
	public void FromEncounterAndFromSavedWorld_MatchPilotRuntime()
	{
		var snapshot = PilotBattleSnapshot.FromSkills(
			"pilot-1",
			[EPilotSkillId.IonizedPayload]);
		var encounter = new BattleEncounter
		{
			Seed = 1,
			Id = "runtime-parity",
			Objective = GrimSpace.Battle.Objectives.EObjective.EliminateOpponents,
			Spawns =
			[
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("player", EType.Fighter),
					Pilot = snapshot,
					Team = ETeam.Player,
			Faction = EFaction.Player,
					Position = new Coord(5, 5, 5),
					ExecutionAgent = new GrimSpace.Battle.Player.UserExecutionAgent(),
				},
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("enemy", EType.Fighter),
					Team = ETeam.Enemy,
			Faction = EFaction.TheOptimality,
					Position = new Coord(0, 0, 0),
					ExecutionAgent = BattleAgentFactory.Create(EBattleAgentKind.Ai),
				},
			],
		};

		using var fromEncounter = BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
		var saved = fromEncounter.Engine.World.Fork();
		using var fromSaved = BattleOrchestrator.FromSavedWorld(saved, "player", encounter);

		var encounterRuntime = fromEncounter.Engine.ActorRuntimes.For("player");
		var savedRuntime = fromSaved.Engine.ActorRuntimes.For("player");

		Assert.Equal(encounterRuntime.PilotId, savedRuntime.PilotId);
		Assert.Equal(encounterRuntime.PilotCapabilities, savedRuntime.PilotCapabilities);
	}

	[Fact]
	public void LazyRuntime_ForUnknownActor_IsEmptyPilot()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(5, 5, 5)),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));

		var childRuntime = battle.Engine.ActorRuntimes.For("spawn-child");
		Assert.Empty(childRuntime.PilotCapabilities);
	}

	[Fact]
	public void FromEncounter_InstallsOneStickyStartRowPerPassivePerUnit()
	{
		using var battle = BattleOrchestrator.FromEncounter(PassiveEncounter(), gridSize: 12);

		foreach (var unit in battle.Engine.World.UnitRegistry.All)
		{
			Assert.Equal(2, battle.Engine.World.ActivationRoundLists.EntriesFor(unit.State.Id).Count);
			AssertPassiveActivationRows(battle.Engine.World, unit.State.Id);
		}
	}

	[Fact]
	public void Load_ReinstallsActivationRowsFromEncounterWithoutResettingSavedState()
	{
		var encounter = PassiveEncounter();
		using var original = BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
		var player = original.Engine.World.StateOf(original.PlayerId);
		player.DefensiveManeuversMpUsed = 2;
		player.ShieldDynamoGrantedPerFace = 10;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			player.CurrentShieldPoints[face] = 40;
		var registry = PersistenceRegistry.CreateDefault();

		var restoredWorld = SaveDtoMapper.RestoreBattleWorld(
			SaveDtoMapper.CaptureBattleWorld(original.Engine.World, registry),
			registry);
		var restoredEncounter = SaveDtoMapper.RestoreBattleEncounter(
			SaveDtoMapper.CaptureBattleEncounter(encounter));

		foreach (var unit in restoredWorld.UnitRegistry.All)
		{
			Assert.True(BattleStatuses.HasDefensiveManeuvers(unit.State));
			Assert.True(BattleStatuses.HasShieldDynamo(unit.State));
			Assert.Empty(restoredWorld.ActivationRoundLists.EntriesFor(unit.State.Id));
		}

		using var restored = BattleOrchestrator.FromSavedWorld(
			restoredWorld,
			original.PlayerId,
			restoredEncounter);

		foreach (var unit in restored.Engine.World.UnitRegistry.All)
		{
			Assert.Equal(2, restoredWorld.ActivationRoundLists.EntriesFor(unit.State.Id).Count);
			AssertPassiveActivationRows(restoredWorld, unit.State.Id);
		}
		var restoredPlayer = restoredWorld.StateOf(original.PlayerId);
		Assert.Equal(2, restoredPlayer.DefensiveManeuversMpUsed);
		Assert.Equal(10, restoredPlayer.ShieldDynamoGrantedPerFace);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(40, restoredPlayer.CurrentShieldPoints[face]);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(3)]
	public void Reinstall_ReplacesRowsWithoutDuplicatingOrRemovingOtherWork(int repetitions)
	{
		using var battle = BattleOrchestrator.FromEncounter(PassiveEncounter(), gridSize: 12);
		var world = battle.Engine.World;
		var unitId = battle.PlayerId;
		var runtime = battle.Engine.ActorRuntimes.For(unitId);
		var previousRows = world.ActivationRoundLists.EntriesFor(unitId).ToArray();
		var otherUnitRows = world.ActivationRoundLists.EntriesFor("enemy").ToArray();
		new ScheduleActivationRoundEffect(
			EUnitActivationPhase.Start, 2, false, new ClearMarkWeakspotAction(unitId))
			.Apply(world, runtime, unitId);
		var scheduledRow = world.ActivationRoundLists.EntriesFor(unitId).Last();

		for (var i = 0; i < repetitions; i++)
			BattlePassiveStatusInstaller.InstallFromCapabilities(world, unitId, runtime);

		var rows = world.ActivationRoundLists.EntriesFor(unitId);
		Assert.Equal(3, rows.Count);
		AssertPassiveActivationRows(world, unitId);
		Assert.Same(scheduledRow, Assert.Single(rows, row => row.Action is ClearMarkWeakspotAction));
		foreach (var previous in previousRows)
			Assert.DoesNotContain(rows, row => ReferenceEquals(previous, row));
		Assert.Equal(otherUnitRows, world.ActivationRoundLists.EntriesFor("enemy"));
		Assert.Equal(2, world.StateOf(unitId).Statuses.Count(status =>
			status.InstanceId == BattleStatuses.PassiveInstanceId(
				nameof(EPilotCapabilityId.StationaryDamageDeflection), unitId)
			|| status.InstanceId == BattleStatuses.PassiveInstanceId(
				nameof(EPilotCapabilityId.ShieldDynamo), unitId)));
	}

	[Fact]
	public void Reinstall_RemovesRowsForCapabilitiesNoLongerPresent()
	{
		using var battle = BattleOrchestrator.FromEncounter(PassiveEncounter(), gridSize: 12);
		var world = battle.Engine.World;
		var unitId = battle.PlayerId;

		BattlePassiveStatusInstaller.InstallFromCapabilities(
			world,
			unitId,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.DefensiveManeuvers])));

		Assert.True(BattleStatuses.HasDefensiveManeuvers(world.StateOf(unitId)));
		Assert.False(BattleStatuses.HasShieldDynamo(world.StateOf(unitId)));
		Assert.IsType<SyncDefensiveManeuversMpUsedAction>(
			Assert.Single(world.ActivationRoundLists.EntriesFor(unitId)).Action);

		BattlePassiveStatusInstaller.InstallFromCapabilities(world, unitId, ActorRuntime.From(null));

		Assert.False(BattleStatuses.HasDefensiveManeuvers(world.StateOf(unitId)));
		Assert.False(BattleStatuses.HasShieldDynamo(world.StateOf(unitId)));
		Assert.Empty(world.ActivationRoundLists.EntriesFor(unitId));
		AssertPassiveActivationRows(world, "enemy");
	}

	[Fact]
	public void Fork_CopiesActivationRowsAndKeepsReinstallationIsolated()
	{
		using var battle = BattleOrchestrator.FromEncounter(PassiveEncounter(), gridSize: 12);
		var world = battle.Engine.World;
		var fork = world.Fork();

		foreach (var unit in fork.UnitRegistry.All)
			AssertPassiveActivationRows(fork, unit.State.Id);

		BattlePassiveStatusInstaller.InstallFromCapabilities(fork, battle.PlayerId, ActorRuntime.From(null));

		Assert.Empty(fork.ActivationRoundLists.EntriesFor(battle.PlayerId));
		AssertPassiveActivationRows(fork, "enemy");
		AssertPassiveActivationRows(world, battle.PlayerId);
	}

	[Fact]
	public void Install_RequiresUnitAlreadyRegisteredInWorld()
	{
		using var battle = BattleOrchestrator.FromEncounter(PassiveEncounter(), gridSize: 12);

		Assert.Throws<KeyNotFoundException>(() =>
			BattlePassiveStatusInstaller.InstallFromCapabilities(
				battle.Engine.World,
				"missing",
				battle.Engine.ActorRuntimes.For(battle.PlayerId)));
		Assert.Empty(battle.Engine.World.ActivationRoundLists.EntriesFor("missing"));
	}

	private static void AssertPassiveActivationRows(BattleWorld world, string unitId)
	{
		var rows = world.ActivationRoundLists.EntriesFor(unitId);
		var sync = Assert.Single(rows, row => row.Action is SyncDefensiveManeuversMpUsedAction);
		var expire = Assert.Single(rows, row => row.Action is ExpireShieldDynamoGrantsAction);
		Assert.Equal(unitId, Assert.IsType<SyncDefensiveManeuversMpUsedAction>(sync.Action).ActorId);
		Assert.Equal(unitId, Assert.IsType<ExpireShieldDynamoGrantsAction>(expire.Action).ActorId);
		foreach (var row in new[] { sync, expire })
		{
			Assert.Equal(EUnitActivationPhase.Start, row.RunAt);
			Assert.Null(row.RoundsRemaining);
			Assert.True(row.Sticky);
		}
		Assert.True(BattleStatuses.HasDefensiveManeuvers(world.StateOf(unitId)));
		Assert.True(BattleStatuses.HasShieldDynamo(world.StateOf(unitId)));
	}

	private static BattleEncounter PassiveEncounter() => new()
	{
		Seed = 1,
		Id = "passive-activation",
		Objective = GrimSpace.Battle.Objectives.EObjective.EliminateOpponents,
		Spawns =
		[
			new BattleSpawn
			{
				Ship = ShipInstance.FromCatalog("player", EType.Fighter),
				Pilot = PilotBattleSnapshot.FromSkills(
					"player-pilot", [EPilotSkillId.DefensiveManeuvers, EPilotSkillId.ShieldDynamo]),
				Team = ETeam.Player,
			Faction = EFaction.Player,
				Position = new Coord(5, 5, 5),
				ExecutionAgent = new GrimSpace.Battle.Player.UserExecutionAgent(),
			},
			new BattleSpawn
			{
				Ship = ShipInstance.FromCatalog("enemy", EType.Fighter),
				Pilot = PilotBattleSnapshot.FromSkills(
					"enemy-pilot", [EPilotSkillId.DefensiveManeuvers, EPilotSkillId.ShieldDynamo]),
				Team = ETeam.Enemy,
			Faction = EFaction.TheOptimality,
				Position = Coord.Zero,
				ExecutionAgent = BattleAgentFactory.Create(EBattleAgentKind.Ai),
			},
		],
	};
}
