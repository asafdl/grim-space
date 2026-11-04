using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ThirstForScrapTests
{
	[Fact]
	public void SkillIsLevelFourAndRogueOnly()
	{
		var skill = PilotSkillCatalog.Get(EPilotSkillId.ThirstForScrap);

		Assert.Equal("Thirst For Scrap", skill.Name);
		Assert.Equal(4, skill.MinimumEarnedLevel);
		Assert.Equal(PilotSkillAudience.ForOperatorClass(EOperatorClassId.Rogue).Kind, skill.Audience.Kind);
		Assert.NotNull(skill.Audience.OperatorClassIds);
		Assert.Equal(EOperatorClassId.Rogue, Assert.Single(skill.Audience.OperatorClassIds));
		Assert.Equal(EPilotCapabilityId.ThirstForScrap, Assert.Single(skill.CapabilityIds));
	}

	[Theory]
	[InlineData("player", 1)]
	[InlineData("child", 1)]
	[InlineData("grandchild", 1)]
	[InlineData("ally", 0)]
	public void KillCreditFollowsSourceOwnershipNotSquadMembership(string sourceId, int expectedStacks)
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(sourceId);
		world.StateOf("enemy").CurrentHullPoints = 50;
		world.StateOf("enemy").CurrentShieldPoints.Fill(0);
		var effect = Damage(world, sourceId, ["enemy"]);

		var records = effect.Apply(world, runtime, sourceId);

		Assert.Equal(expectedStacks, world.StateOf("player").ThirstForScrapStacks);
		Assert.Equal(expectedStacks, records.OfType<Record<ThirstForScrapChangedFacts>>().Count());
		effect.Undo(world, runtime, sourceId);
		Assert.Equal(0, world.StateOf("player").ThirstForScrapStacks);
		Assert.True(world.StateOf("enemy").IsAlive);
	}

	[Theory]
	[InlineData("player")]
	[InlineData("child")]
	public void FriendlyKillDoesNotGrantStack(string sourceId)
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		world.StateOf("ally").CurrentHullPoints = 50;
		world.StateOf("ally").CurrentShieldPoints.Fill(0);

		var records = Damage(world, sourceId, ["ally"])
			.Apply(world, battle.Engine.ActorRuntimes.For(sourceId), sourceId);

		Assert.False(world.StateOf("ally").IsAlive);
		Assert.Equal(0, world.StateOf("player").ThirstForScrapStacks);
		Assert.Empty(records.OfType<Record<ThirstForScrapChangedFacts>>());
	}

	[Fact]
	public void AreaMultiKillGrantsOneStackPerTargetAndUndoRestoresAll()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For("player");
		foreach (var id in new[] { "enemy", "second-enemy" })
		{
			world.StateOf(id).CurrentHullPoints = 50;
			world.StateOf(id).CurrentShieldPoints.Fill(0);
		}
		var effect = Damage(world, "player", ["enemy", "second-enemy"]);

		var records = effect.Apply(world, runtime, "player");

		Assert.Equal(2, world.StateOf("player").ThirstForScrapStacks);
		Assert.Equal([1, 2], records.OfType<Record<ThirstForScrapChangedFacts>>()
			.Select(record => record.Value.Stacks));
		effect.Undo(world, runtime, "player");
		Assert.Equal(0, world.StateOf("player").ThirstForScrapStacks);
		Assert.Equal(50, world.StateOf("enemy").CurrentHullPoints);
		Assert.Equal(50, world.StateOf("second-enemy").CurrentHullPoints);
	}

	[Fact]
	public void ChainKillGrantsOneStackEvenWithRepeatedTargetHops()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var enemy = world.StateOf("enemy");
		enemy.CurrentHullPoints = 50;
		enemy.CurrentShieldPoints.Fill(0);
		var effect = new ApplyKillTriggersEffect(new ApplyChainLightningDamageEffect(
			EImpactCause.LightningCannonBurst, new HashSet<Coord>(),
			[new("enemy", enemy.Position), new("enemy", enemy.Position)],
			100, 0, world.StateOf("player").Position));
		var runtime = battle.Engine.ActorRuntimes.For("player");

		var records = effect.Apply(world, runtime, "player");

		Assert.Equal(1, world.StateOf("player").ThirstForScrapStacks);
		Assert.Single(records.OfType<Record<ThirstForScrapChangedFacts>>());
		effect.Undo(world, runtime, "player");
		Assert.Equal(0, world.StateOf("player").ThirstForScrapStacks);
		Assert.Equal(50, enemy.CurrentHullPoints);
	}

	[Fact]
	public void SelfDestructingChildStillCreditsItsLivingOwner()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		world.StateOf("enemy").CurrentHullPoints = 50;
		world.StateOf("enemy").CurrentShieldPoints.Fill(0);
		world.StateOf("child").CurrentShieldPoints.Fill(0);
		var effect = new ApplyKillTriggersEffect(new ApplyAreaDamageEffect(
			EImpactCause.VoidBombBlast,
			new HashSet<Coord> { world.StateOf("enemy").Position, world.StateOf("child").Position },
			100, world.StateOf("child").Position, destroySource: true));

		effect.Apply(world, battle.Engine.ActorRuntimes.For("child"), "child");

		Assert.False(world.StateOf("child").IsAlive);
		Assert.Equal(1, world.StateOf("player").ThirstForScrapStacks);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(3)]
	[InlineData(20)]
	public void DamageBonusIsAdditiveUncappedAndLeavesOtherEffectsUnchanged(int stacks)
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		world.StateOf("player").ThirstForScrapStacks = stacks;
		var heal = new ApplyAreaAllyHealEffect(EImpactCause.ScrapDroneSwarmBurst, new HashSet<Coord>(), 100);
		var rewritten = BattleStatuses.RewriteOutgoing(new DetonateAction("player"), world,
		[
			new ApplyAreaDamageEffect(EImpactCause.VoidBombBlast, new HashSet<Coord>(), 100, Coord.Zero)
				.WithAddedDamageBonusPercent(80),
			new ApplyChainLightningDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord>(), [], 100, 0, Coord.Zero).WithAddedDamageBonusPercent(80),
			heal,
		]);

		Assert.Equal(80 + stacks * 10, Assert.IsType<ApplyAreaDamageEffect>(rewritten[0]).DamageBonusPercent);
		Assert.Equal(80 + stacks * 10, Assert.IsType<ApplyChainLightningDamageEffect>(rewritten[1]).DamageBonusPercent);
		Assert.Same(heal, rewritten[2]);
	}

	[Fact]
	public void ThreeStacksDealThirtyPercentMoreActualDamage()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		world.StateOf("player").ThirstForScrapStacks = 3;
		var enemy = world.StateOf("enemy");
		enemy.CurrentShieldPoints.Fill(0);
		enemy.CurrentHullPoints = 1000;
		var effect = Assert.Single(BattleStatuses.RewriteOutgoing(new LightningCannonAction("player"), world,
		[
			new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord> { enemy.Position }, 100, world.StateOf("player").Position),
		]));

		effect.Apply(world, battle.Engine.ActorRuntimes.For("player"), "player");

		Assert.Equal(870, enemy.CurrentHullPoints);
	}

	[Fact]
	public void WorldResolvedEffectPassGrantsStacksOnForkWithoutMutatingLiveWorld()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		world.StateOf("enemy").CurrentHullPoints = 50;
		world.StateOf("enemy").CurrentShieldPoints.Fill(0);
		var fork = world.ForkForSimulation();
		var runtime = battle.Engine.ActorRuntimes.For("player");
		var pass = (IResolvedEffectPass<BattleWorld, ActorRuntime>)fork;
		var effect = Assert.IsType<ApplyKillTriggersEffect>(Assert.Single(pass.RewriteResolvedEffects(
			new LightningCannonAction("player"), fork, runtime,
			[new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord> { fork.StateOf("enemy").Position }, 100, fork.StateOf("player").Position)])));

		effect.Apply(fork, runtime, "player");

		Assert.Equal(1, fork.StateOf("player").ThirstForScrapStacks);
		Assert.Equal(0, world.StateOf("player").ThirstForScrapStacks);
		Assert.True(world.StateOf("enemy").IsAlive);
		effect.Undo(fork, runtime, "player");
		Assert.Equal(0, fork.StateOf("player").ThirstForScrapStacks);
	}

	[Fact]
	public void SaveLoadAndReinstallPreserveStacksAndNewBattleStartsAtZero()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		world.StateOf("player").ThirstForScrapStacks = 3;
		var registry = PersistenceRegistry.CreateDefault();
		var unit = SaveDtoMapper.RestoreBattleUnitState(
			SaveDtoMapper.CaptureBattleUnitState(world.StateOf("player")));
		Assert.Equal(3, unit.ThirstForScrapStacks);

		var restored = SaveDtoMapper.RestoreBattleWorld(SaveDtoMapper.CaptureBattleWorld(world, registry), registry);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			restored, "player", battle.Engine.ActorRuntimes.For("player"));

		Assert.Equal(3, restored.StateOf("player").ThirstForScrapStacks);
		Assert.True(BattleStatuses.HasThirstForScrap(restored.StateOf("player")));
		Assert.Single(restored.StateOf("player").Statuses);
		Assert.Empty(restored.ActivationRoundLists.EntriesFor("player"));
		using var fresh = Battle();
		Assert.Equal(0, fresh.Engine.World.StateOf("player").ThirstForScrapStacks);
	}

	[Fact]
	public void ReplayUsesPersistableStackFactsWithoutChangingLiveState()
	{
		using var battle = Battle();
		var state = battle.Engine.World.StateOf("player");
		var replay = new ReplayState(new Dictionary<string, State> { [state.Id] = state });
		var registry = PersistenceRegistry.CreateDefault();
		var fact = Assert.IsType<Record<ThirstForScrapChangedFacts>>(
			registry.Read(registry.Write(new Record<ThirstForScrapChangedFacts>(new(state.Id, 3)))));

		replay.ApplyThirstForScrap(fact.Value);

		Assert.Equal(3, replay.StateOf(state.Id).ThirstForScrapStacks);
		Assert.Equal(0, state.ThirstForScrapStacks);
	}

	private static ApplyKillTriggersEffect Damage(BattleWorld world, string sourceId, string[] targetIds) =>
		new(new ApplyAreaDamageEffect(
			EImpactCause.VoidBombBlast,
			targetIds.Select(id => world.StateOf(id).Position).ToHashSet(),
			100,
			world.StateOf(sourceId).Position));

	private static BattleOrchestrator Battle()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var child = BattleTestFixture.Ally(new Coord(4, 5, 5), "child");
		child.State.ParentId = player.State.Id;
		var grandchild = BattleTestFixture.Ally(new Coord(3, 5, 5), "grandchild");
		grandchild.State.ParentId = child.State.Id;
		var enemy = BattleTestFixture.Enemy(new Coord(5, 7, 5));
		var second = BattleTestFixture.RepurposedMiner(new Coord(5, 8, 5), "second-enemy");
		var ally = BattleTestFixture.Ally(new Coord(6, 5, 5));
		return BattleTestFixture.BeginSimulation(
			player, [enemy, second, child, grandchild, ally],
			pilotsByActorId: new Dictionary<string, PilotBattleSnapshot?>
			{
				[player.State.Id] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.ThirstForScrap]),
			});
	}
}
