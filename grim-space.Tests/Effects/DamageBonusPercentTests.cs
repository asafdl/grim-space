using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class DamageBonusPercentTests
{
	[Fact]
	public void NotFullyFormedIsLevelSixRoguePassive()
	{
		var skill = PilotSkillCatalog.Get(EPilotSkillId.NotFullyFormed);

		Assert.Equal(6, skill.MinimumEarnedLevel);
		Assert.Equal("Not Fully Formed", skill.Name);
		Assert.Equal(EOperatorClassId.Rogue, Assert.Single(skill.Audience.OperatorClassIds!));
		Assert.Equal(EPilotCapabilityId.SpawnedUnitDamageBonus, Assert.Single(skill.CapabilityIds));
	}

	[Fact]
	public void ApplyAreaDamage_AppliesDamageBonusPercent()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(6, 5, 5));
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			enemy.State.CurrentShieldPoints[face] = 0;
		var hullBefore = enemy.State.CurrentHullPoints;
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { enemy.State.Position },
			10,
			player.State.Position)
			.WithDamageBonusPercent(80);

		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(player.State.Id),
			player.State.Id);

		Assert.Equal(hullBefore - 18, enemy.State.CurrentHullPoints);
	}

	[Fact]
	public void NotFullyFormedAddsThirtyPercentAgainstParentedUnitsForAreaAndChainDamage()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var regular = BattleTestFixture.Enemy(new Coord(5, 7, 5));
		var spawned = BattleTestFixture.RepurposedMiner(new Coord(5, 8, 5), "spawned");
		spawned.State.ParentId = regular.State.Id;
		using var battle = BattleTestFixture.BeginSimulation(
			player,
			[regular, spawned],
			pilotsByActorId: new Dictionary<string, PilotBattleSnapshot?>
			{
				[player.State.Id] = PilotBattleSnapshot.FromSkills(
					"pilot",
					[EPilotSkillId.NotFullyFormed]),
			});
		var world = battle.Engine.World;
		foreach (var target in new[] { regular.State, spawned.State })
		{
			target.CurrentHullPoints = 1000;
			target.CurrentShieldPoints.Fill(0);
		}

		var area = Assert.IsType<ApplyAreaDamageEffect>(Assert.Single(BattleStatuses.RewriteOutgoing(
			new LightningCannonAction(player.State.Id),
			world,
			[
				new ApplyAreaDamageEffect(
					EImpactCause.LightningCannonBurst,
					new HashSet<Coord> { regular.State.Position, spawned.State.Position },
					100,
					player.State.Position),
			])));
		area.Apply(world, battle.Engine.ActorRuntimes.For(player.State.Id), player.State.Id);

		Assert.Equal(900, regular.State.CurrentHullPoints);
		Assert.Equal(870, spawned.State.CurrentHullPoints);
		area.Undo(world, battle.Engine.ActorRuntimes.For(player.State.Id), player.State.Id);

		var chain = Assert.IsType<ApplyChainLightningDamageEffect>(Assert.Single(BattleStatuses.RewriteOutgoing(
			new LightningCannonAction(player.State.Id),
			world,
			[
				new ApplyChainLightningDamageEffect(
					EImpactCause.LightningCannonBurst,
					new HashSet<Coord>(),
					[
						new ChainLightningHop(regular.State.Id, regular.State.Position),
						new ChainLightningHop(spawned.State.Id, spawned.State.Position),
					],
					100,
					0,
					player.State.Position),
			])));
		chain.Apply(world, battle.Engine.ActorRuntimes.For(player.State.Id), player.State.Id);

		Assert.Equal(900, regular.State.CurrentHullPoints);
		Assert.Equal(870, spawned.State.CurrentHullPoints);
	}
}
