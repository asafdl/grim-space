using GrimSpace.Battle;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
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
	public void AreaAndChainHitsRequireZeroEffectiveShieldsBeforeHit(int shields, int temporary, int hullDamage)
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
		var runtime = battle.Engine.ActorRuntimes.For(actor.Id);
		foreach (var effect in RewrittenDamage(battle, target.Id))
		{
			effect.Apply(world, runtime, actor.Id);
			Assert.Equal(1000 - hullDamage, target.CurrentHullPoints);
			effect.Undo(world, runtime, actor.Id);
			Assert.Equal(1000, target.CurrentHullPoints);
			Assert.Equal(shields + temporary, Defense.EffectiveShield(target, face));
		}
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

		RewrittenDamage(battle, target.Id)[0].Apply(world, battle.Engine.ActorRuntimes.For("player"), "player");

		Assert.Equal(860, target.CurrentHullPoints);
	}

	[Fact]
	public void MixedAreaTargetsAndChainHopsEvaluateIndependently()
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
		var effects = BattleStatuses.RewriteOutgoing(new LightningCannonAction(actor.Id), world,
		[
			new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord> { first.Position, second.Position }, 100, actor.Position),
			new ApplyChainLightningDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord>(), [new(first.Id, first.Position), new(second.Id, second.Position)],
				100, 0, actor.Position),
		]);
		foreach (var effect in effects)
		{
			effect.Apply(world, runtime, actor.Id);
			Assert.Equal(860, first.CurrentHullPoints);
			Assert.Equal(910, second.CurrentHullPoints);
			effect.Undo(world, runtime, actor.Id);
			Assert.Equal(1000, first.CurrentHullPoints);
			Assert.Equal(1000, second.CurrentHullPoints);
		}
	}

	[Fact]
	public void AddsToThirstForScrapRatherThanMultiplying()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var runtime = ActorRuntime.From(PilotBattleSnapshot.FromSkills(
			"pilot", [EPilotSkillId.NothingButMetal, EPilotSkillId.ThirstForScrap]));
		BattlePassiveStatusInstaller.InstallFromCapabilities(world, "player", runtime);
		world.StateOf("player").ThirstForScrapStacks = 3;
		var target = world.StateOf("enemy");
		target.CurrentHullPoints = 1000;
		target.CurrentShieldPoints.Fill(0);
		foreach (var effect in RewrittenDamage(battle, target.Id))
		{
			effect.Apply(world, runtime, "player");
			Assert.Equal(830, target.CurrentHullPoints);
			effect.Undo(world, runtime, "player");
		}
	}

	[Fact]
	public void DamageRewritesPreserveConditionalBonusAndExistingFields()
	{
		var area = new ApplyAreaDamageEffect(EImpactCause.HighVelocityRod,
			new HashSet<Coord>(), 100, Coord.Zero, hullBreachDamagePercent: 350)
			.WithSpawnedUnitDamageBonusPercent(30)
			.WithUnshieldedDamageBonusPercent(40);
		Assert.All(new[]
		{
			area.WithAdditionalDamage(10), area.WithAdditionalShieldDamage(10),
			area.WithShieldDamageBonusPercent(20), area.WithAdjacentHitDamagePercent(150),
			area.WithDamageBonusPercent(30), area.WithAddedDamageBonusPercent(80, includesOverdrive: true),
			area.WithSpawnedUnitDamageBonusPercent(30),
		}, rewritten =>
		{
			Assert.Equal(40, rewritten.UnshieldedDamageBonusPercent);
			Assert.Equal(350, rewritten.HullBreachDamagePercent);
			Assert.Equal(30, rewritten.SpawnedUnitDamageBonusPercent);
		});
		var chain = new ApplyChainLightningDamageEffect(EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(), [], 100, 20, Coord.Zero)
			.WithSpawnedUnitDamageBonusPercent(30).WithUnshieldedDamageBonusPercent(40);
		Assert.All(new[]
		{
			chain.WithAdditionalShieldDamage(10), chain.WithShieldDamageBonusPercent(20),
			chain.WithDamageBonusPercent(30), chain.WithAddedDamageBonusPercent(80, includesOverdrive: true),
			chain.WithSpawnedUnitDamageBonusPercent(30),
		}, rewritten =>
		{
			Assert.Equal(40, rewritten.UnshieldedDamageBonusPercent);
			Assert.Equal(20, rewritten.ChainFalloffPercentPerHop);
			Assert.Equal(30, rewritten.SpawnedUnitDamageBonusPercent);
		});
	}

	[Fact]
	public void ReinstallForkAndSaveLoadPreservePassiveWithoutActivationWork()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		BattlePassiveStatusInstaller.InstallFromCapabilities(world, "player", battle.Engine.ActorRuntimes.For("player"));
		Assert.Single(world.StateOf("player").Statuses);
		Assert.Empty(world.ActivationRoundLists.EntriesFor("player"));
		var registry = PersistenceRegistry.CreateDefault();
		var restored = SaveDtoMapper.RestoreBattleWorld(SaveDtoMapper.CaptureBattleWorld(world, registry), registry);
		var unit = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(world.StateOf("player")));
		Assert.NotNull(Assert.Single(unit.Statuses).RewriteOutgoing);
		foreach (var snapshot in new[] { world.Fork(), restored })
		{
			var effect = Assert.IsType<ApplyAreaDamageEffect>(Assert.Single(BattleStatuses.RewriteOutgoing(
				new LightningCannonAction("player"), snapshot,
				[new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst, new HashSet<Coord>(), 100, Coord.Zero)])));
			Assert.Equal(40, effect.UnshieldedDamageBonusPercent);
		}
	}

	private static IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> RewrittenDamage(
		BattleOrchestrator battle, string targetId)
	{
		var world = battle.Engine.World;
		var actor = world.StateOf("player");
		var target = world.StateOf(targetId);
		return BattleStatuses.RewriteOutgoing(new LightningCannonAction(actor.Id), world,
		[
			new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord> { target.Position }, 100, actor.Position),
			new ApplyChainLightningDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord>(), [new ChainLightningHop(target.Id, target.Position)], 100, 0, actor.Position),
		]);
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
