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
		var action = new DetonateAction(sourceId);
		var records = new List<IRecord>();
		var effects = AttackEffects.Area(
			world,
			sourceId,
			EImpactCause.VoidBombBlast,
			new HashSet<Coord> { world.StateOf("enemy").Position },
			100,
			world.StateOf(sourceId).Position).ToArray();

		BattleEffectTestKit.ApplyEffects(action, world, runtime, effects, records);

		Assert.Equal(expectedStacks, world.StateOf("player").StatusOf<ThirstForScrap>()!.Stacks);
		Assert.Equal(expectedStacks, records.OfType<Record<ThirstForScrapChangedFacts>>().Count());
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
		var action = new DetonateAction(sourceId);
		var records = new List<IRecord>();
		var effects = AttackEffects.Area(
			world,
			sourceId,
			EImpactCause.VoidBombBlast,
			new HashSet<Coord> { world.StateOf("ally").Position },
			100,
			world.StateOf(sourceId).Position,
			alliesTakeDamage: true).ToArray();

		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			battle.Engine.ActorRuntimes.For(sourceId),
			effects,
			records);

		Assert.False(world.StateOf("ally").IsAlive);
		Assert.Equal(0, world.StateOf("player").StatusOf<ThirstForScrap>()!.Stacks);
		Assert.Empty(records.OfType<Record<ThirstForScrapChangedFacts>>());
	}

	[Fact]
	public void AreaMultiKillGrantsOneStackPerTarget()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For("player");
		foreach (var id in new[] { "enemy", "second-enemy" })
		{
			world.StateOf(id).CurrentHullPoints = 50;
			world.StateOf(id).CurrentShieldPoints.Fill(0);
		}
		var action = new DetonateAction("player");
		var records = new List<IRecord>();
		var effects = AttackEffects.Area(
			world,
			"player",
			EImpactCause.VoidBombBlast,
			new HashSet<Coord>
			{
				world.StateOf("enemy").Position,
				world.StateOf("second-enemy").Position,
			},
			100,
			world.StateOf("player").Position).ToArray();

		BattleEffectTestKit.ApplyEffects(action, world, runtime, effects, records);

		Assert.Equal(2, world.StateOf("player").StatusOf<ThirstForScrap>()!.Stacks);
		Assert.Equal([1, 2], records.OfType<Record<ThirstForScrapChangedFacts>>()
			.Select(record => record.Value.Stacks));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(3)]
	public void StacksAddDamageBonusPercent(int stacks)
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For("player");
		world.StateOf("player").StatusOf<ThirstForScrap>()!.Stacks = stacks;
		var enemy = world.StateOf("enemy");
		var prepared = BattleEffectTestKit.PrepareDamage(
			new DetonateAction("player"),
			world,
			runtime,
			new DamageUnitEffect(EImpactCause.VoidBombBlast, enemy.Id, world.StateOf("player").Position, 100));

		Assert.Equal(
			stacks * PilotSkillPercents.ThirstForScrap.DamageBonusPercentPerKill,
			prepared.BonusPercent);
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

		Assert.Equal(3, replay.StateOf(state.Id).StatusOf<ThirstForScrap>()!.Stacks);
		Assert.Equal(0, state.StatusOf<ThirstForScrap>()!.Stacks);
	}

	private static BattleOrchestrator Battle()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var child = BattleTestFixture.Ally(new Coord(4, 5, 5), "child");
		child.State.ParentId = player.State.Id;
		var grandchild = BattleTestFixture.Ally(new Coord(3, 5, 5), "grandchild");
		grandchild.State.ParentId = child.State.Id;
		var ally = BattleTestFixture.Ally(new Coord(5, 4, 5), "ally");
		var enemy = BattleTestFixture.Enemy(new Coord(5, 6, 5));
		var second = BattleTestFixture.RepurposedMiner(new Coord(6, 6, 5), "second-enemy");
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[child, grandchild, ally, enemy, second],
			pilotsByActorId: new Dictionary<string, PilotBattleSnapshot?>
			{
				[player.State.Id] = PilotBattleSnapshot.FromSkills(
					"pilot",
					[EPilotSkillId.ThirstForScrap]),
			});
		return battle;
	}
}
