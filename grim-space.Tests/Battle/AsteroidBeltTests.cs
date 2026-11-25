using GrimSpace.Battle;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Hazards;

[BattleTestSuite]
public sealed class AsteroidBeltTests
{
	[Fact]
	public void ReplacingCoverInForkDoesNotChangeLiveOrSiblingCover()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var live = battle.Engine.World;
		var cell = new Coord(4, 5, 5);
		live.AddNonUnit(AsteroidBelt.Create("asteroidbelt-test", cell, live.Grid, 80));
		var fork = live.ForkForSimulation();
		var sibling = live.ForkForSimulation();

		Assert.True(fork.RemoveNonUnit("asteroidbelt-test"));
		fork.AddNonUnit(AsteroidBelt.Create("asteroidbelt-test", cell, fork.Grid, 40));

		Assert.Equal(80, live.AsteroidBeltAt(cell)!.DamageResistancePercent);
		Assert.Equal(80, sibling.AsteroidBeltAt(cell)!.DamageResistancePercent);
		Assert.Equal(40, fork.AsteroidBeltAt(cell)!.DamageResistancePercent);
	}

	[Fact]
	public void CoverDoesNotBlockMovementOrRodFootprint()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			ShipInstance.FromCatalog("player", EType.Rogue),
			ETeam.Player,
			origin,
			new UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(origin + Coord.Up * 2);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			blocked: new HashSet<Coord>());
		var world = battle.Engine.World;
		var moveCell = origin + Coord.Forward;
		var shotCell = origin + Coord.Up;
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-move", moveCell, world.Grid, 40));
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-shot", shotCell, world.Grid, 40));

		Assert.True(MoveDef.Instance.IsPossible(
			new MoveStepAction(player.State.Id),
			world,
			battle.PlayerAgent.Sim.RuntimeFor(player.State.Id)));
		Assert.Contains(
			shotCell,
			HighVelocityRodDef.Instance.AffectedCells(
				new HighVelocityRodAction(player.State.Id, ESpatialOrientation.Dorsal),
				world));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(50)]
	public void CreateRejectsUnsupportedResistance(int resistancePercent)
	{
		var grid = BattleTestFixture.Grid();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			AsteroidBelt.Create("asteroidbelt-invalid", new Coord(5, 5, 5), grid, resistancePercent));
	}

	[Fact]
	public void CreateRejectsCellsOutsideTheGrid()
	{
		var grid = BattleTestFixture.Grid();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			AsteroidBelt.Create("asteroidbelt-invalid", new Coord(-1, 5, 5), grid, 40));
	}

	[Fact]
	public void CoverReducesOutgoingAndIncomingDamage()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For("player");
		var target = world.UnitRegistry.UnitOf("enemy");
		var attackOrigin = world.StateOf("player").Position;
		world.AddNonUnit(AsteroidBelt.Create(
			"asteroidbelt-through-shot",
			attackOrigin + Coord.Forward,
			world.Grid,
			40));

		Assert.Equal(100, EffectiveDamage(target, world, runtime, attackOrigin, 100));

		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-source", attackOrigin, world.Grid, 40));
		Assert.Equal(30, EffectiveDamage(target, world, runtime, attackOrigin, 100));

		world.AddNonUnit(AsteroidBelt.Create(
			"asteroidbelt-target",
			target.State.Position,
			world.Grid,
			AsteroidBelt.HighResistancePercent));
		Assert.Equal(6, EffectiveDamage(target, world, runtime, attackOrigin, 100));
	}

	[Fact]
	public void TargetCoverUsesItsCurrentResistanceForEachAttack()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var world = battle.Engine.World;
		var target = world.UnitRegistry.UnitOf("enemy");
		var cell = target.State.Position;
		var origin = world.StateOf("player").Position;
		var runtime = battle.Engine.ActorRuntimes.For("player");
		world.AddNonUnit(AsteroidBelt.Create(
			"asteroidbelt-target",
			cell,
			world.Grid,
			AsteroidBelt.HighResistancePercent));

		Assert.Equal(20, EffectiveDamage(target, world, runtime, origin, 100));
		_ = new ErodeAsteroidBeltCoverEffect(new HashSet<Coord> { cell })
			.Apply(world, runtime, "player");
		Assert.Equal(60, EffectiveDamage(target, world, runtime, origin, 100));
		_ = new ErodeAsteroidBeltCoverEffect(new HashSet<Coord> { cell })
			.Apply(world, runtime, "player");
		Assert.Null(world.AsteroidBeltAt(cell));
		Assert.Equal(100, EffectiveDamage(target, world, runtime, origin, 100));
	}

	[Fact]
	public void ProtectionDroneAndCoverReduceBothHitChannelsMultiplicatively()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For("player");
		var target = world.UnitRegistry.UnitOf("enemy");
		world.AddNonUnit(AsteroidBelt.Create(
			"asteroidbelt-target",
			target.State.Position,
			world.Grid,
			AsteroidBelt.HighResistancePercent));
		target.State.Statuses.Add(new ProtectionDroneBuff());

		Assert.Equal(18, EffectiveDamage(target, world, runtime, world.StateOf("player").Position, 100));
	}

	[Fact]
	public void LightningAttackDegradesEmptyCoverAndUndoRestoresIt()
	{
		var sim = BattleTestFixture.LightningCannonDuel(0, 4);
		var world = sim.World;
		const string actorId = "enemy";
		var action = new LightningCannonAction(actorId);
		var runtime = sim.RuntimeFor(actorId);
		var announce = Assert.Single(
			LightningCannonDef.Instance.Resolve(action, world, runtime)
				.OfType<AnnounceAreaDamageEffect>());
		var emptyCell = announce.Facts.Cells.First(cell =>
			cell != announce.Facts.Origin
			&& world.UnitRegistry.All.All(unit => !unit.State.IsAlive || unit.State.Position != cell));
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-empty", emptyCell, world.Grid, 80));

		var effects = ExecutionHelper.ApplyAndResolve(action, world, runtime, sim.Rules);

		Assert.Equal(40, world.AsteroidBeltAt(emptyCell)!.DamageResistancePercent);
		ExecutionHelper.UndoEffects(effects, action, world, runtime);
		Assert.Equal(80, world.AsteroidBeltAt(emptyCell)!.DamageResistancePercent);
	}

	[Fact]
	public void LightningAttacksErodeEachIntersectedPatchOnceAndUndoAndRedoDestruction()
	{
		var sim = BattleTestFixture.LightningCannonDuel(0, 4);
		var world = sim.World;
		const string actorId = "enemy";
		var action = new LightningCannonAction(actorId);
		var runtime = sim.RuntimeFor(actorId);
		var announce = Assert.Single(
			LightningCannonDef.Instance.Resolve(action, world, runtime)
				.OfType<AnnounceAreaDamageEffect>());
		var targetCell = Assert.Single(announce.Facts.ChainHops!);
		Assert.Contains(targetCell, announce.Facts.Cells);
		var emptyCells = announce.Facts.Cells
			.Where(cell => cell != announce.Facts.Origin && cell != targetCell)
			.Where(cell => world.UnitRegistry.All.All(unit =>
				!unit.State.IsAlive || unit.State.Position != cell))
			.Take(2)
			.ToArray();
		Assert.Equal(2, emptyCells.Length);

		var sourceCell = announce.Facts.Origin;
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-source", sourceCell, world.Grid, 80));
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-empty-a", emptyCells[0], world.Grid, 80));
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-empty-b", emptyCells[1], world.Grid, 80));
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-target", targetCell, world.Grid, 80));

		var firstRecords = new List<IRecord>();
		var firstEffects = ExecutionHelper.ApplyAndResolve(action, world, runtime, sim.Rules, firstRecords);
		var firstFacts = firstRecords.OfType<Record<AsteroidBeltChangedFacts>>().ToArray();
		Assert.Equal(
			new[] { 40, 40, 40 },
			new[] { emptyCells[0], emptyCells[1], targetCell }
				.Select(cell => world.AsteroidBeltAt(cell)!.DamageResistancePercent));
		Assert.Equal(80, world.AsteroidBeltAt(sourceCell)!.DamageResistancePercent);
		Assert.Equal(
			new[] { "asteroidbelt-empty-a", "asteroidbelt-empty-b", "asteroidbelt-target" },
			firstFacts.Select(record => record.Value.HazardId));
		Assert.All(firstFacts, record =>
		{
			Assert.Equal(actorId, record.Value.SourceId);
			Assert.Equal(80, record.Value.BeforeResistancePercent);
			Assert.Equal(40, record.Value.AfterResistancePercent);
		});
		var lastImpactIndex = firstRecords.FindLastIndex(record => record is Record<ImpactFacts>);
		var coverChangeIndex = firstRecords.FindIndex(record => record is Record<AsteroidBeltChangedFacts>);
		Assert.True(lastImpactIndex >= 0);
		Assert.True(coverChangeIndex > lastImpactIndex);

		var secondRecords = new List<IRecord>();
		var secondEffects = ExecutionHelper.ApplyAndResolve(action, world, runtime, sim.Rules, secondRecords);
		var secondFacts = secondRecords.OfType<Record<AsteroidBeltChangedFacts>>().ToArray();
		Assert.All(
			new[] { emptyCells[0], emptyCells[1], targetCell },
			cell => Assert.Null(world.AsteroidBeltAt(cell)));
		Assert.Equal(3, secondFacts.Length);
		Assert.All(secondFacts,
			record => Assert.Equal(0, record.Value.AfterResistancePercent));

		ExecutionHelper.UndoEffects(secondEffects, action, world, runtime);
		Assert.All(
			new[] { emptyCells[0], emptyCells[1], targetCell },
			cell => Assert.Equal(40, world.AsteroidBeltAt(cell)!.DamageResistancePercent));

		var redoRecords = new List<IRecord>();
		foreach (var effect in secondEffects)
			redoRecords.AddRange(effect.Apply(world, runtime, actorId));
		Assert.All(
			new[] { emptyCells[0], emptyCells[1], targetCell },
			cell => Assert.Null(world.AsteroidBeltAt(cell)));
		Assert.Equal(
			secondFacts.Select(record => record.Value),
			redoRecords.OfType<Record<AsteroidBeltChangedFacts>>()
				.Select(record => record.Value));

		ExecutionHelper.UndoEffects(secondEffects, action, world, runtime);
		ExecutionHelper.UndoEffects(firstEffects, action, world, runtime);
		Assert.All(
			new[] { emptyCells[0], emptyCells[1], targetCell },
			cell => Assert.Equal(80, world.AsteroidBeltAt(cell)!.DamageResistancePercent));
		Assert.Equal(80, world.AsteroidBeltAt(sourceCell)!.DamageResistancePercent);
	}

	[Fact]
	public void ResolvedReplayKeepsSegmentStartCoverAndRecordsErosion()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = TurnOrchestrationTests.EnemyInLightningCannonLine(playerPos);
		using var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, enemyPos);
		var live = battle.Engine.World;
		live.AddNonUnit(AsteroidBelt.Create("asteroidbelt-target", enemyPos, live.Grid, 80));
		BattleTestFixture.ResetPlayerPlanning(battle);
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new LightningCannonAction("player")));

		var replay = BattleTestActions.CommitAndResolve(battle);

		var start = Assert.Single(replay.StartAsteroidBelts);
		Assert.Equal(80, start.DamageResistancePercent);
		var change = replay.History.OfType<Record<AsteroidBeltChangedFacts>>()
			.First(record => record.Value.SourceId == "player").Value;
		Assert.Equal((80, 40), (change.BeforeResistancePercent, change.AfterResistancePercent));
		Assert.True(live.AsteroidBeltAt(enemyPos) is null or { DamageResistancePercent: < 80 });
	}

	[Fact]
	public void AreaDamageDegradesEmptyCover()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			ShipInstance.FromCatalog("player", EType.Rogue),
			ETeam.Player,
			origin,
			new UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(origin + Coord.Up * 2);
		var battle = BattleTestFixture.BeginSimulation(player, enemy, blocked: new HashSet<Coord>());
		var world = battle.Engine.World;
		var coverCell = origin + Coord.Up;
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-area", coverCell, world.Grid, 80));
		var action = new HighVelocityRodAction(player.State.Id, ESpatialOrientation.Dorsal);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		var effects = ExecutionHelper.ApplyAndResolve(action, world, runtime, battle.Engine.Rules);

		Assert.Equal(40, world.AsteroidBeltAt(coverCell)!.DamageResistancePercent);
		ExecutionHelper.UndoEffects(effects, action, world, runtime);
		Assert.Equal(80, world.AsteroidBeltAt(coverCell)!.DamageResistancePercent);
	}

	[Fact]
	public void FriendlyFireImmunityDoesNotProtectCoverFromAnAttack()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var ally = BattleTestFixture.Ally(origin + Coord.Up, "ally");
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[ally],
			blocked: new HashSet<Coord>());
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		var cell = ally.State.Position;
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-friendly", cell, world.Grid, 80));
		var allyHull = world.StateOf(ally.State.Id).CurrentHullPoints;
		var effects = AttackEffects.Area(
				world,
				player.State.Id,
				EImpactCause.ScrapDroneSwarmBurst,
				new HashSet<Coord> { cell },
				100,
				origin,
				alliesTakeDamage: false)
			.Concat(AttackEffects.Erosion(world, [cell], origin))
			.ToArray();

		foreach (var effect in effects)
			_ = effect.Apply(world, runtime, player.State.Id);

		Assert.Equal(allyHull, world.StateOf(ally.State.Id).CurrentHullPoints);
		Assert.Equal(40, world.AsteroidBeltAt(cell)!.DamageResistancePercent);
	}

	[Fact]
	public void MovementHealingBuffsAndZeroDamageEffectsDoNotErodeCover()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For("player");
		var moveCell = origin + Coord.Forward;
		world.AddNonUnit(AsteroidBelt.Create("asteroidbelt-safe", moveCell, world.Grid, 80));

		_ = ExecutionHelper.ApplyAndResolve(new MoveStepAction("player"), world, runtime, battle.Engine.Rules);

		Assert.Equal(80, world.AsteroidBeltAt(moveCell)!.DamageResistancePercent);

		var targetCell = world.StateOf("enemy").Position;
		var nonDamageEffects = new IEffect<BattleWorld, ActorRuntime>[]
		{
			new ApplyAreaAllyHealEffect(
				EImpactCause.ScrapDroneSwarmBurst,
				new HashSet<Coord> { targetCell },
				20),
			new SetProtectionDroneBuffActiveEffect("enemy", true),
		};
		foreach (var effect in nonDamageEffects)
			_ = effect.Apply(world, runtime, "player");
		Assert.Equal(80, world.AsteroidBeltAt(moveCell)!.DamageResistancePercent);
	}

	[Fact]
	public void ErodingCoverInOneSimulationDoesNotChangeLiveOrSiblingWorlds()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var live = battle.Engine.World;
		var cell = new Coord(4, 5, 5);
		live.AddNonUnit(AsteroidBelt.Create("asteroidbelt-sim", cell, live.Grid, 80));
		var sim = live.ForkForSimulation();
		var sibling = live.ForkForSimulation();
		var effect = new ErodeAsteroidBeltCoverEffect(new HashSet<Coord> { cell });

		_ = effect.Apply(sim, battle.Engine.ActorRuntimes.For("player"), "player");

		Assert.Equal(80, live.AsteroidBeltAt(cell)!.DamageResistancePercent);
		Assert.Equal(80, sibling.AsteroidBeltAt(cell)!.DamageResistancePercent);
		Assert.Equal(40, sim.AsteroidBeltAt(cell)!.DamageResistancePercent);
	}

	private static int EffectiveDamage(
		Unit target,
		BattleWorld world,
		ActorRuntime runtime,
		Coord origin,
		int damage)
	{
		var prepared = BattleEffectTestKit.PrepareDamage(
			new HighVelocityRodAction("player", ESpatialOrientation.Forward),
			world,
			runtime,
			new DamageUnitEffect(EImpactCause.HighVelocityRod, target.State.Id, origin, damage));
		var result = Defense.ScalePercent(prepared.Damage, 100 + prepared.BonusPercent);
		foreach (var percent in prepared.DamagePercents)
			result = Defense.ScalePercent(result, percent);
		foreach (var step in prepared.HitSteps)
		{
			if (step.AddedShieldDamagePercent != 0)
				continue;
			result = Defense.ScalePercent(result, step.RetainPercent);
		}
		return result;
	}
}
