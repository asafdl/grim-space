using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class NothingButMetalTests
{
	[Fact]
	public void SkillIsLevelFiveRogueOnly()
	{
		var skill = PilotSkillCatalog.Get(EPilotSkillId.NothingButMetal);
		Assert.Equal("Nothing But Metal", skill.Name);
		Assert.Equal(5, skill.MinimumEarnedLevel);
		Assert.NotNull(skill.Audience.OperatorClassIds);
		Assert.Equal(EOperatorClassId.Rogue, Assert.Single(skill.Audience.OperatorClassIds));
		Assert.Equal(EPilotCapabilityId.NothingButMetal, Assert.Single(skill.CapabilityIds));
	}

	[Theory]
	[InlineData(0, 0, 140)]
	[InlineData(1, 0, 99)]
	[InlineData(50, 0, 50)]
	[InlineData(200, 0, 0)]
	[InlineData(0, 1, 99)]
	[InlineData(0, 50, 50)]
	public void HitsRequireZeroEffectiveShieldsBeforeHit(int shields, int temporary, int hullDamage)
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var target = world.StateOf("enemy");
		var actor = world.StateOf("player");
		target.CurrentHullPoints = 1000;
		target.CurrentShieldPoints.Fill(shields);
		if (temporary > 0)
			TemporaryShields.GrantPerFace(target, "grant", temporary);
		var face = BodyFrame.From(target).HitFaceFrom(actor.Position);
		var action = new LightningCannonAction(actor.Id);
		var runtime = battle.Engine.ActorRuntimes.For(actor.Id);
		var applied = BattleEffectTestKit.ApplyEffects(
			action,
			world,
			runtime,
			[
				new DamageUnitEffect(
					EImpactCause.LightningCannonBurst,
					target.Id,
					actor.Position,
					100),
			]);
		Assert.Equal(1000 - hullDamage, target.CurrentHullPoints);
		ExecutionHelper.UndoEffects(applied, action, world, runtime);
		Assert.Equal(1000, target.CurrentHullPoints);
		Assert.Equal(shields + temporary, Defense.EffectiveShield(target, face));
	}

	[Fact]
	public void OnlyTheHitFaceMatters()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var target = world.StateOf("enemy");
		target.CurrentShieldPoints.Fill(200);
		target.CurrentShieldPoints[BodyFrame.From(target).HitFaceFrom(world.StateOf("player").Position)] = 0;
		target.CurrentHullPoints = 1000;

		BattleEffectTestKit.ApplyEffects(
			new LightningCannonAction("player"),
			world,
			battle.Engine.ActorRuntimes.For("player"),
			[
				new DamageUnitEffect(
					EImpactCause.LightningCannonBurst,
					target.Id,
					world.StateOf("player").Position,
					100),
			]);

		Assert.Equal(860, target.CurrentHullPoints);
	}

	[Fact]
	public void MixedAreaTargetsEvaluateIndependently()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var actor = world.StateOf("player");
		var first = world.StateOf("enemy");
		var second = world.StateOf("second-enemy");
		first.CurrentHullPoints = second.CurrentHullPoints = 1000;
		first.CurrentShieldPoints.Fill(0);
		second.CurrentShieldPoints.Fill(10);
		var runtime = battle.Engine.ActorRuntimes.For(actor.Id);
		var action = new LightningCannonAction(actor.Id);
		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			runtime,
			AttackEffects.Area(
				world,
				actor.Id,
				EImpactCause.LightningCannonBurst,
				new HashSet<Coord> { first.Position, second.Position },
				100,
				actor.Position));

		Assert.Equal(860, first.CurrentHullPoints);
		Assert.Equal(910, second.CurrentHullPoints);
	}

	[Fact]
	public void AddsToThirstForScrapRatherThanMultiplying()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var runtime = ActorRuntime.From(PilotBattleSnapshot.FromSkills(
			"pilot", [EPilotSkillId.NothingButMetal, EPilotSkillId.ThirstForScrap]));
		BattleStatusMaterializer.Materialize(world, "player", runtime);
		world.StateOf("player").StatusOf<ThirstForScrap>()!.Stacks = 3;
		var target = world.StateOf("enemy");
		target.CurrentHullPoints = 1000;
		target.CurrentShieldPoints.Fill(0);
		BattleEffectTestKit.ApplyEffects(
			new LightningCannonAction("player"),
			world,
			runtime,
			[
				new DamageUnitEffect(
					EImpactCause.LightningCannonBurst,
					target.Id,
					world.StateOf("player").Position,
					100),
			]);
		Assert.Equal(830, target.CurrentHullPoints);
	}

	[Fact]
	public void ReinstallForkAndSaveLoadPreservePassiveWithoutActivationWork()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		BattleStatusMaterializer.Materialize(world, "player", battle.Engine.ActorRuntimes.For("player"));
		Assert.Single(world.StateOf("player").Statuses);
		Assert.Empty(world.ActivationRoundLists.EntriesFor("player"));
		var registry = PersistenceRegistry.CreateDefault();
		var restored = SaveDtoMapper.RestoreBattleWorld(SaveDtoMapper.CaptureBattleWorld(world, registry), registry);
		var unit = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(world.StateOf("player")));
		Assert.Contains(unit.Statuses, status => status is NothingButMetal);
		foreach (var snapshot in new[] { world.Fork(), restored })
		{
			Assert.True(snapshot.StateOf("player").Has<NothingButMetal>());
			snapshot.StateOf("enemy").CurrentShieldPoints.Fill(0);
			var prepared = BattleEffectTestKit.PrepareDamage(
				new LightningCannonAction("player"),
				snapshot,
				battle.Engine.ActorRuntimes.For("player"),
				new DamageUnitEffect(
					EImpactCause.LightningCannonBurst,
					"enemy",
					snapshot.StateOf("player").Position,
					100));
			Assert.Equal(
				PilotSkillPercents.NothingButMetal.DamageBonusPercent,
				prepared.BonusPercent);
		}
	}

	private static BattleOrchestrator Battle() =>
		BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(5, 5, 5)),
			[
				BattleTestFixture.Enemy(new Coord(6, 5, 5)),
				BattleTestFixture.RepurposedMiner(new Coord(7, 5, 5), "second-enemy"),
			],
			pilotsByActorId: new Dictionary<string, PilotBattleSnapshot?>
			{
				["player"] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.NothingButMetal]),
			});
}
