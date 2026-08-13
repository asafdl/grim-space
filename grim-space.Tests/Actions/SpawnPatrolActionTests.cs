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
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class SpawnPatrolActionTests
{
	[Fact]
	public void DeploySpawnsPatrolWithParentIdAndSetsCooldown()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(new SpawnPatrolAction(carrierId)));

		var carrier = sim.StateOf<ActorState>(carrierId);
		Assert.Equal(CombatConfig.PatrolCooldownTurns, carrier.PatrolSpawnCooldownRemaining);

		var patrol = Assert.Single(
			UnitRegistry.For(sim.World).All,
			unit => unit.State.Type == EType.Patrol);
		Assert.Equal(carrierId, patrol.State.ParentId);
		Assert.Equal(ETeam.Enemy, patrol.Alliance.Team);

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

		Assert.True(sim.TryEnqueue(new SpawnPatrolAction(carrierId)));
		Assert.Single(UnitRegistry.For(sim.World).All, unit => unit.State.Type == EType.Patrol);
		Assert.Equal(CombatConfig.PatrolCooldownTurns, sim.StateOf<ActorState>(carrierId).PatrolSpawnCooldownRemaining);

		Assert.True(sim.TryUndoLast());

		Assert.DoesNotContain(UnitRegistry.For(sim.World).All, unit => unit.State.Type == EType.Patrol);
		Assert.Equal(0, sim.StateOf<ActorState>(carrierId).PatrolSpawnCooldownRemaining);
	}

	[Fact]
	public void DeployIllegalWhileCooldownActive()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(new SpawnPatrolAction(carrierId)));
		Assert.False(sim.TryEnqueue(new SpawnPatrolAction(carrierId)));
	}

	[Fact]
	public void DeployIllegalAtLivingCap()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();
		FillLivingPatrols(sim.World, carrierId, CombatConfig.MaxLivingPatrolChildren);

		sim.World.StateOf(carrierId).PatrolSpawnCooldownRemaining = 0;
		Assert.False(sim.TryEnqueue(new SpawnPatrolAction(carrierId, "patrol-overflow")));
	}

	[Fact]
	public void LivingCapFreesSlotWhenPatrolDies()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();
		FillLivingPatrols(sim.World, carrierId, CombatConfig.MaxLivingPatrolChildren);

		var doomed = UnitRegistry.For(sim.World).All.First(unit => unit.State.Type == EType.Patrol);
		doomed.State.HullPoints = 0;
		sim.World.StateOf(carrierId).PatrolSpawnCooldownRemaining = 0;

		Assert.True(sim.TryEnqueue(new SpawnPatrolAction(carrierId, "patrol-replacement")));
	}

	[Fact]
	public void RoundUpkeepDecrementsPatrolCooldown()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		battle.Engine.World.StateOf(carrierId).PatrolSpawnCooldownRemaining = 2;

		battle.Engine.Commit([new RoundUpkeepAction(carrierId)]);

		Assert.Equal(1, battle.Engine.World.StateOf(carrierId).PatrolSpawnCooldownRemaining);
	}

	[Fact]
	public void CommitRecordsSpawnFacts()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		battle.Engine.Commit([new SpawnPatrolAction(carrierId)]);

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
			Encounter.DevDefault(seed: 3, gridSize: 16),
			gridSize: 16);

		foreach (var unit in UnitRegistry.For(battle.Engine.World).All)
			Assert.Equal(EntityIds.System, unit.State.ParentId);
	}

	private static BattleOrchestrator CarrierBattle(Coord carrierPos) =>
		BattleTestFixture.BeginCarrierVsPlayer(new Coord(0, 5, 5), carrierPos);

	private static void FillLivingPatrols(BattleWorld world, string carrierId, int count)
	{
		var carrier = UnitRegistry.For(world).UnitOf(carrierId);
		for (var i = 0; i < count; i++)
		{
			var patrol = Factory.Create(
				new Instance
				{
					Id = $"patrol-{i}",
					Type = EType.Patrol,
					Alliance = carrier.Alliance,
				},
				new Coord(1 + i, 1, 5),
				new AiController(),
				world.IdRegistry,
				initialMomentum: 0,
				Coord.Forward,
				Coord.Up,
				parentId: carrierId);
			UnitRegistry.For(world).Add(patrol);
		}
	}
}
