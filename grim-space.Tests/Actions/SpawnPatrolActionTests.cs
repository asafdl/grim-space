using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Ids;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class SpawnPatrolActionTests
{
	[Fact]
	public void DeploySpawnsPatrolWithParentIdAndSetsCooldown()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(SpawnPatrolDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)));

		var carrier = sim.StateOf<ActorState>(carrierId);
		Assert.Equal(
			CatalogExpectations.DefaultPatrolBaySpec().CooldownTurns,
			StateMountTestKit.CooldownRemaining(carrier, EAbilityKind.PatrolBay));

		var patrol = Assert.Single(
			UnitRegistry.For(sim.World).All,
			unit => unit.State.Type == EType.Patrol);
		Assert.Equal(carrierId, patrol.State.ParentId);
		Assert.Equal(ETeam.Enemy, patrol.Team);

		var frame = BodyFrame.From(carrier);
		Assert.Equal(carrier.Position + frame.Step(ESpatialOrientation.Ventral), patrol.State.Position);
		Assert.Equal(carrier.Fore, patrol.State.Fore);
		Assert.Equal(carrier.Dorsal, patrol.State.Dorsal);
	}

	[Fact]
	public void UndoRemovesSpawnedPatrolAndRestoresCarrierCooldown()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(SpawnPatrolDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)));
		Assert.Single(UnitRegistry.For(sim.World).All, unit => unit.State.Type == EType.Patrol);
		Assert.Equal(
			CatalogExpectations.DefaultPatrolBaySpec().CooldownTurns,
			StateMountTestKit.CooldownRemaining(sim.StateOf<ActorState>(carrierId), EAbilityKind.PatrolBay));

		Assert.True(sim.TryUndoLast());

		Assert.DoesNotContain(UnitRegistry.For(sim.World).All, unit => unit.State.Type == EType.Patrol);
		Assert.Equal(0, StateMountTestKit.CooldownRemaining(sim.StateOf<ActorState>(carrierId), EAbilityKind.PatrolBay));
	}

	[Fact]
	public void QueuedPatrolKeepsSpawnedIdAcrossReevaluationForkAndReplay()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();
		var action = SpawnPatrolDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral);

		Assert.True(sim.TryEnqueue(action));
		AssertSpawned(sim.World, action.SpawnedUnitId);

		sim.Reevaluate();
		AssertSpawned(sim.World, action.SpawnedUnitId);
		AssertSpawned(sim.Fork().World, action.SpawnedUnitId);
		AssertSpawned(sim.ReplayWorld(sim.Actions.Count), action.SpawnedUnitId);
	}

	[Fact]
	public void DeployIllegalWhileCooldownActive()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(SpawnPatrolDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)));
		Assert.False(sim.TryEnqueue(SpawnPatrolDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)));
	}

	[Fact]
	public void DeployIllegalAtLivingCap()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();
		FillLivingPatrols(sim.World, carrierId, CatalogExpectations.DefaultPatrolBaySpec().MaxLivingChildren);

		StateMountTestKit.SetCooldownRemaining(sim.World.StateOf(carrierId), EAbilityKind.PatrolBay, 0);
		Assert.False(sim.TryEnqueue(new SpawnPatrolAction(
			carrierId,
			ESpatialOrientation.Ventral,
			"patrol-overflow")));
	}

	[Fact]
	public void LivingCapFreesSlotWhenPatrolDies()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();
		FillLivingPatrols(sim.World, carrierId, CatalogExpectations.DefaultPatrolBaySpec().MaxLivingChildren);

		var doomed = UnitRegistry.For(sim.World).All.First(unit => unit.State.Type == EType.Patrol);
		doomed.State.HullPoints = 0;
		StateMountTestKit.SetCooldownRemaining(sim.World.StateOf(carrierId), EAbilityKind.PatrolBay, 0);

		Assert.True(sim.TryEnqueue(new SpawnPatrolAction(
			carrierId,
			ESpatialOrientation.Ventral,
			"patrol-replacement")));
	}

	[Fact]
	public void RoundUpkeepDecrementsPatrolCooldown()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		StateMountTestKit.SetCooldownRemaining(battle.Engine.World.StateOf(carrierId), EAbilityKind.PatrolBay, 2);

		battle.Engine.Commit([new RoundUpkeepAction(carrierId)]);

		Assert.Equal(1, StateMountTestKit.CooldownRemaining(battle.Engine.World.StateOf(carrierId), EAbilityKind.PatrolBay));
	}

	[Fact]
	public void CommitRecordsSpawnFacts()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		battle.Engine.Commit([SpawnPatrolDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)]);

		var patrol = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.Patrol);
		Assert.Contains(
			battle.Engine.History(),
			entry => entry is Record<SpawnFacts> { Value: var spawn }
				&& spawn.SourceId == carrierId
				&& spawn.TargetId == patrol.State.Id
				&& spawn.EntityType == EType.Patrol);
	}

	[Fact]
	public void CarrierAbilitiesIncludeDeploy()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Carrier);

		Assert.Contains(abilities, def => def is RailgunDef);
		Assert.Contains(abilities, def => def is SpawnPatrolDef);
	}

	[Fact]
	public void EncounterUnitsUseSystemParentId()
	{
		var battle = BattleOrchestrator.FromEncounter(
			BattleEncounter.DevDefault(seed: 3, gridSize: 16),
			gridSize: 16);

		foreach (var unit in UnitRegistry.For(battle.Engine.World).All)
			Assert.Equal(BattleActorIds.Rules, unit.State.ParentId);
	}

	private static BattleOrchestrator CarrierBattle(Coord carrierPos) =>
		BattleTestFixture.BeginCarrierVsPlayer(new Coord(0, 5, 5), carrierPos);

	private static void FillLivingPatrols(BattleWorld world, string carrierId, int count)
	{
		var carrier = UnitRegistry.For(world).UnitOf(carrierId);
		for (var i = 0; i < count; i++)
		{
			var patrol = Factory.Create(
				ShipInstance.FromCatalog($"patrol-{i}", EType.Patrol),
				carrier.Team,
				new Coord(1 + i, 1, 5),
				new AiController(),
				Coord.Forward,
				Coord.Up,
				parentId: carrierId);
			UnitRegistry.For(world).Add(patrol);
		}
	}

	private static void AssertSpawned(BattleWorld world, string unitId) =>
		Assert.True(UnitRegistry.For(world).TryGet(unitId, out _));
}
