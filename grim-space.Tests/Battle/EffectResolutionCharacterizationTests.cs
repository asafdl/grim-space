using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class EffectResolutionCharacterizationTests
{
	private const string PlayerId = "player";
	private const string EnemyId = "enemy";
	private const string SecondId = "second-enemy";

	[Fact]
	public void AreaHitResolvesEachTargetWithItsOwnStatusesFaceAndShields()
	{
		using var battle = CreateBattle();
		var world = battle.Engine.World;
		var origin = world.StateOf(PlayerId).Position;
		var enemy = world.StateOf(EnemyId);
		var second = world.StateOf(SecondId);
		var enemyFace = BodyFrame.From(enemy).HitFaceFrom(origin);
		var secondFace = BodyFrame.From(second).HitFaceFrom(origin);
		enemy.CurrentShieldPoints[enemyFace] = 30;
		second.Statuses.Add(BattleStatusMaterializer.Create(EPilotCapabilityId.DamageResistance));
		var action = new HighVelocityRodAction(PlayerId, ESpatialOrientation.Forward);
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var records = new List<IRecord>();
		var effects = AttackEffects.Area(
			world,
			PlayerId,
			EImpactCause.HighVelocityRod,
			new HashSet<Coord> { enemy.Position, second.Position },
			100,
			origin).ToArray();

		var applied = BattleEffectTestKit.ApplyEffects(action, world, runtime, effects, records);

		Assert.IsType<Record<AreaDamageFacts>>(records[0]);
		var impacts = records.OfType<Record<ImpactFacts>>().Select(record => record.Value).ToArray();
		Assert.Equal(
			[
				new ImpactFacts(PlayerId, EnemyId, EImpactCause.HighVelocityRod, enemyFace, 30, 70, 30, 1000),
				new ImpactFacts(PlayerId, SecondId, EImpactCause.HighVelocityRod, secondFace, 0, 95, 0, 1000),
			],
			impacts);
		ExecutionHelper.UndoEffects(applied, action, world, runtime);
		Assert.Equal((30, 1000), (enemy.CurrentShieldPoints[enemyFace], enemy.CurrentHullPoints));
		Assert.Equal(1000, second.CurrentHullPoints);
	}

	[Theory]
	[InlineData(100, 8, 0)]
	[InlineData(6, 6, 0)]
	[InlineData(2, 2, 3)]
	public void ShieldBonusPercentRoundsAwayFromZeroAndNeverReachesHull(
		int shields,
		int expectedShieldDamage,
		int expectedHullDamage)
	{
		using var battle = CreateBattle();
		var world = battle.Engine.World;
		var origin = world.StateOf(PlayerId).Position;
		var enemy = world.StateOf(EnemyId);
		var face = BodyFrame.From(enemy).HitFaceFrom(origin);
		enemy.CurrentShieldPoints[face] = shields;
		var action = new HighVelocityRodAction(PlayerId, ESpatialOrientation.Forward);
		var records = new List<IRecord>();
		var damage = new DamageUnitEffect(EImpactCause.HighVelocityRod, EnemyId, origin, 5)
		{
			ShieldDamageBonusPercent = 50,
		};

		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			battle.Engine.ActorRuntimes.For(PlayerId),
			[damage],
			records);

		var impact = Assert.Single(records.OfType<Record<ImpactFacts>>()).Value;
		Assert.Equal((expectedShieldDamage, expectedHullDamage), (impact.ShieldDamage, impact.HullDamage));
	}

	[Fact]
	public void ThirstForScrapStackFromFirstTargetBoostsLaterTargetOfSameAttack()
	{
		using var battle = CreateBattle();
		var world = battle.Engine.World;
		world.StateOf(PlayerId).Statuses.Add(BattleStatusMaterializer.Create(EPilotCapabilityId.ThirstForScrap));
		world.StateOf(EnemyId).CurrentHullPoints = 50;
		var action = new HighVelocityRodAction(PlayerId, ESpatialOrientation.Forward);
		var origin = world.StateOf(PlayerId).Position;
		var effects = AttackEffects.Area(
			world,
			PlayerId,
			EImpactCause.HighVelocityRod,
			new HashSet<Coord>
			{
				world.StateOf(EnemyId).Position,
				world.StateOf(SecondId).Position,
			},
			100,
			origin).ToArray();

		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			battle.Engine.ActorRuntimes.For(PlayerId),
			effects);

		Assert.Equal(1, world.StateOf(PlayerId).StatusOf<ThirstForScrap>()!.Stacks);
		Assert.Equal(
			1000 - Defense.ScalePercent(100, 100 + PilotSkillPercents.ThirstForScrap.DamageBonusPercentPerKill),
			world.StateOf(SecondId).CurrentHullPoints);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void OverdriveBonusAppliesExactlyOnceToAttacksRegardlessOfCrossingTiming(bool startsInOverdrive)
	{
		using var battle = CreateBattle();
		var world = battle.Engine.World;
		var actor = world.StateOf(PlayerId);
		Assert.Equal(0, actor.Loadout.WeaponDamagePercent);
		var damage = new DamageUnitEffect(
			EImpactCause.HighVelocityRod,
			EnemyId,
			actor.Position,
			100);
		IEffect<BattleWorld, ActorRuntime>[] effects;
		if (startsInOverdrive)
		{
			actor.Overdrive = true;
			effects = [damage];
		}
		else
		{
			actor.Heat = actor.HeatProfile.EffectiveCapacity - HeatChangeEffect.PerAbilityUse;
			effects = [new HeatChangeEffect(HeatChangeEffect.PerAbilityUse), damage];
		}

		var action = new HighVelocityRodAction(PlayerId, ESpatialOrientation.Forward);
		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			battle.Engine.ActorRuntimes.For(PlayerId),
			effects);

		Assert.True(actor.Overdrive);
		Assert.Equal(
			1000 - Defense.ScalePercent(100, 100 + HeatChangeEffect.OverdriveDamageBonusPercent),
			world.StateOf(EnemyId).CurrentHullPoints);
	}

	private static BattleOrchestrator CreateBattle()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var enemy = BattleTestFixture.Enemy(new Coord(5, 7, 5));
		var second = BattleTestFixture.RepurposedMiner(new Coord(5, 8, 5), SecondId);
		foreach (var state in new[] { enemy.State, second.State })
		{
			state.CurrentHullPoints = 1000;
			state.CurrentShieldPoints.Fill(0);
		}

		return BattleTestFixture.BeginSimulation(player, [enemy, second]);
	}
}
