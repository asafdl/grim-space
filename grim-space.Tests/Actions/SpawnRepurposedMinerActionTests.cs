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
public sealed class SpawnRepurposedMinerActionTests
{
	[Fact]
	public void DeploySpawnsRepurposedMinerWithParentIdAndSetsCooldown()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(SpawnRepurposedMinerDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)));

		var carrier = sim.StateOf<ActorState>(carrierId);
		Assert.Equal(
			CatalogExpectations.DefaultMinerBaySpec().CooldownTurns,
			StateMountTestKit.CooldownRemaining(carrier, EAbilityKind.MinerBay));

		var repurposedMiner = Assert.Single(
			UnitRegistry.For(sim.World).All,
			unit => unit.State.Type == EType.RepurposedMiner);
		Assert.Equal(carrierId, repurposedMiner.State.ParentId);
		Assert.Equal(ETeam.Enemy, repurposedMiner.Team);

		var frame = BodyFrame.From(carrier);
		Assert.Equal(carrier.Position + frame.Step(ESpatialOrientation.Ventral), repurposedMiner.State.Position);
		Assert.Equal(carrier.Fore, repurposedMiner.State.Fore);
		Assert.Equal(carrier.Dorsal, repurposedMiner.State.Dorsal);
	}

	[Fact]
	public void UndoRemovesSpawnedRepurposedMinerAndRestoresCarrierCooldown()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(SpawnRepurposedMinerDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)));
		Assert.Single(UnitRegistry.For(sim.World).All, unit => unit.State.Type == EType.RepurposedMiner);
		Assert.Equal(
			CatalogExpectations.DefaultMinerBaySpec().CooldownTurns,
			StateMountTestKit.CooldownRemaining(sim.StateOf<ActorState>(carrierId), EAbilityKind.MinerBay));

		Assert.True(sim.TryUndoLast());

		Assert.DoesNotContain(UnitRegistry.For(sim.World).All, unit => unit.State.Type == EType.RepurposedMiner);
		Assert.Equal(0, StateMountTestKit.CooldownRemaining(sim.StateOf<ActorState>(carrierId), EAbilityKind.MinerBay));
	}

	[Fact]
	public void QueuedRepurposedMinerKeepsSpawnedIdAcrossReevaluationForkAndReplay()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();
		var action = SpawnRepurposedMinerDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral);

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

		Assert.True(sim.TryEnqueue(SpawnRepurposedMinerDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)));
		Assert.False(sim.TryEnqueue(SpawnRepurposedMinerDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)));
	}

	[Fact]
	public void DeployIllegalAtLivingCap()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();
		FillLivingRepurposedMiners(sim.World, carrierId, CatalogExpectations.DefaultMinerBaySpec().MaxLivingChildren);

		StateMountTestKit.SetCooldownRemaining(sim.World.StateOf(carrierId), EAbilityKind.MinerBay, 0);
		Assert.False(sim.TryEnqueue(new SpawnRepurposedMinerAction(
			carrierId,
			ESpatialOrientation.Ventral,
			"repurposed-miner-overflow")));
	}

	[Fact]
	public void LivingCapFreesSlotWhenRepurposedMinerDies()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var sim = battle.Engine.CreateSimulation();
		FillLivingRepurposedMiners(sim.World, carrierId, CatalogExpectations.DefaultMinerBaySpec().MaxLivingChildren);

		var doomed = UnitRegistry.For(sim.World).All.First(unit => unit.State.Type == EType.RepurposedMiner);
		doomed.State.HullPoints = 0;
		StateMountTestKit.SetCooldownRemaining(sim.World.StateOf(carrierId), EAbilityKind.MinerBay, 0);

		Assert.True(sim.TryEnqueue(new SpawnRepurposedMinerAction(
			carrierId,
			ESpatialOrientation.Ventral,
			"repurposed-miner-replacement")));
	}

	[Fact]
	public void RoundUpkeepDecrementsRepurposedMinerCooldown()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		StateMountTestKit.SetCooldownRemaining(battle.Engine.World.StateOf(carrierId), EAbilityKind.MinerBay, 2);

		battle.Engine.Commit([new RoundUpkeepAction(carrierId)]);

		Assert.Equal(1, StateMountTestKit.CooldownRemaining(battle.Engine.World.StateOf(carrierId), EAbilityKind.MinerBay));
	}

	[Fact]
	public void CommitRecordsSpawnFacts()
	{
		var battle = CarrierBattle(new Coord(5, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		battle.Engine.Commit([SpawnRepurposedMinerDef.Instance.Bind(carrierId, ESpatialOrientation.Ventral)]);

		var repurposedMiner = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.RepurposedMiner);
		Assert.Contains(
			battle.Engine.History(),
			entry => entry is Record<SpawnFacts> { Value: var spawn }
				&& spawn.SourceId == carrierId
				&& spawn.TargetId == repurposedMiner.State.Id
				&& spawn.EntityType == EType.RepurposedMiner);
	}

	[Fact]
	public void CarrierAbilitiesIncludeDeploy()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Carrier);

		Assert.Contains(abilities, def => def is LightningCannonDef);
		Assert.Contains(abilities, def => def is SpawnRepurposedMinerDef);
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

	private static void FillLivingRepurposedMiners(BattleWorld world, string carrierId, int count)
	{
		var carrier = UnitRegistry.For(world).UnitOf(carrierId);
		for (var i = 0; i < count; i++)
		{
			var repurposedMiner = Factory.Create(
				ShipInstance.FromCatalog($"repurposed-miner-{i}", EType.RepurposedMiner),
				carrier.Team,
				new Coord(1 + i, 1, 5),
				new AiController(),
				Coord.Forward,
				Coord.Up,
				parentId: carrierId);
			UnitRegistry.For(world).Add(repurposedMiner);
		}
	}

	private static void AssertSpawned(BattleWorld world, string unitId) =>
		Assert.True(UnitRegistry.For(world).TryGet(unitId, out _));
}
