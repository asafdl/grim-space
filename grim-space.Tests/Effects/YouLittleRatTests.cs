using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class YouLittleRatTests
{
	[Fact]
	public void SkillIsLevelFourRoguePassive()
	{
		var skill = PilotSkillCatalog.Get(EPilotSkillId.YouLittleRat);
		Assert.Equal(4, skill.MinimumEarnedLevel);
		Assert.Equal("You Little Rat", skill.Name);
		Assert.NotNull(skill.Audience.OperatorClassIds);
		Assert.Equal(EOperatorClassId.Rogue, Assert.Single(skill.Audience.OperatorClassIds));
		Assert.Equal(EPilotCapabilityId.YouLittleRat, Assert.Single(skill.CapabilityIds));
	}

	[Theory]
	[InlineData(5, 0, 0, 100)]
	[InlineData(6, 0, 0, 80)]
	[InlineData(7, 0, 0, 80)]
	[InlineData(2, 2, 1, 100)]
	[InlineData(2, 2, 2, 80)]
	public void AreaAndChainDamageUseInclusiveManhattanThreshold(int x, int y, int z, int damage)
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var target = world.StateOf("player");
		var source = world.StateOf("enemy");
		source.Position = target.Position + new Coord(x, y, z);
		target.CurrentShieldPoints.Fill(0);
		target.CurrentHullPoints = 1000;
		var action = new HighVelocityRodAction(source.Id, ESpatialOrientation.Forward);
		var effect = new DamageUnitEffect(
			EImpactCause.HighVelocityRod,
			target.Id,
			source.Position,
			100);
		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			battle.Engine.ActorRuntimes.For(source.Id),
			[effect]);
		Assert.Equal(1000 - damage, target.CurrentHullPoints);
	}

	[Theory]
	[InlineData(1, 7, 80)]
	[InlineData(7, 1, 100)]
	public void UsesAttackOriginNotCurrentSourcePositionOrOwner(int sourceDistance, int originDistance, int damage)
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var target = world.StateOf("player");
		var source = world.StateOf("enemy");
		source.Position = target.Position + new Coord(sourceDistance, 0, 0);
		source.ParentId = target.Id;
		target.CurrentHullPoints = 1000;
		target.CurrentShieldPoints.Fill(0);
		var origin = target.Position + new Coord(originDistance, 0, 0);
		var action = new HighVelocityRodAction(source.Id, ESpatialOrientation.Forward);
		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			battle.Engine.ActorRuntimes.For(source.Id),
			[
				new DamageUnitEffect(
					EImpactCause.VoidBombBlast,
					target.Id,
					origin,
					100),
			]);
		Assert.Equal(1000 - damage, target.CurrentHullPoints);
	}

	[Fact]
	public void ResistanceScalesShieldBonusAndHullAndComposesWithOtherStatuses()
	{
		using var battle = Battle();
		var world = battle.Engine.World;
		var target = world.StateOf("player");
		world.StateOf("enemy").Position = target.Position + new Coord(6, 0, 0);
		var runtime = battle.Engine.ActorRuntimes.For("enemy");
		target.CurrentShieldPoints.Fill(200);
		var action = new HighVelocityRodAction("enemy", ESpatialOrientation.Forward);
		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			runtime,
			[
				new DamageUnitEffect(
					EImpactCause.HighVelocityRod,
					target.Id,
					world.StateOf("enemy").Position,
					100) { ShieldDamageBonusPercent = 50 },
			]);

		Assert.Equal(80, Enum.GetValues<ESpatialOrientation>().Min(face => target.CurrentShieldPoints[face]));
		target.CurrentShieldPoints.Fill(0);
		target.CurrentHullPoints = 1000;
		target.Statuses.Add(BattleStatusMaterializer.Create(EPilotCapabilityId.ReinforcedHull));
		BattleEffectTestKit.ApplyEffects(
			action,
			world,
			runtime,
			[
				new DamageUnitEffect(
					EImpactCause.HighVelocityRod,
					target.Id,
					world.StateOf("enemy").Position,
					100),
			]);
		Assert.Equal(946, target.CurrentHullPoints);
	}

	[Fact]
	public void ReinstallForkAndSaveLoadPreserveResistanceWithoutActivationWork()
	{
		using var battle = Battle();
		var runtime = battle.Engine.ActorRuntimes.For("player");
		var world = battle.Engine.World;
		BattleStatusMaterializer.Materialize(world, "player", runtime);
		Assert.Single(world.StateOf("player").Statuses);
		Assert.Empty(world.ActivationRoundLists.EntriesFor("player"));
		var registry = PersistenceRegistry.CreateDefault();
		var restored = SaveDtoMapper.RestoreBattleWorld(SaveDtoMapper.CaptureBattleWorld(world, registry), registry);
		var unit = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(world.StateOf("player")));
		foreach (var state in new[] { restored.StateOf("player"), world.Fork().StateOf("player"), unit })
			Assert.Contains(state.Statuses, status => status is YouLittleRat);

		var prepared = BattleEffectTestKit.PrepareDamage(
			new HighVelocityRodAction("enemy", ESpatialOrientation.Forward),
			restored,
			battle.Engine.ActorRuntimes.For("enemy"),
			new DamageUnitEffect(
				EImpactCause.HighVelocityRod,
				"player",
				restored.StateOf("enemy").Position,
				100) { ShieldDamageBonusPercent = 50 });
		Assert.Equal(
			[new HitStep(PilotSkillPercents.YouLittleRat.DamageTakenPercent)],
			prepared.HitSteps);
	}

	private static BattleOrchestrator Battle() =>
		BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(1, 1, 1)),
			BattleTestFixture.Enemy(new Coord(7, 1, 1)),
			pilotsByActorId: new Dictionary<string, PilotBattleSnapshot?>
			{
				["player"] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.YouLittleRat]),
			});
}
