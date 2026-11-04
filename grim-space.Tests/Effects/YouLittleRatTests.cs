using GrimSpace.Battle;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Core.Actions;
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
		var runtime = battle.Engine.ActorRuntimes.For(source.Id);
		IEffect<GrimSpace.Battle.World.BattleWorld, ActorRuntime>[] effects =
		[
			new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord> { target.Position }, 100, source.Position),
			new ApplyChainLightningDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord>(), [new ChainLightningHop(target.Id, target.Position)],
				100, 0, source.Position),
		];
		foreach (var effect in effects)
		{
			effect.Apply(world, runtime, source.Id);
			Assert.Equal(1000 - damage, target.CurrentHullPoints);
			effect.Undo(world, runtime, source.Id);
			Assert.Equal(1000, target.CurrentHullPoints);
		}
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
		IEffect<GrimSpace.Battle.World.BattleWorld, ActorRuntime>[] effects =
		[
			new ApplyAreaDamageEffect(EImpactCause.VoidBombBlast,
				new HashSet<Coord> { target.Position }, 100, origin),
			new ApplyChainLightningDamageEffect(EImpactCause.LightningCannonBurst,
				new HashSet<Coord>(), [new ChainLightningHop(target.Id, target.Position)], 100, 0, origin),
		];
		foreach (var effect in effects)
		{
			effect.Apply(world, battle.Engine.ActorRuntimes.For(source.Id), source.Id);
			Assert.Equal(1000 - damage, target.CurrentHullPoints);
			effect.Undo(world, battle.Engine.ActorRuntimes.For(source.Id), source.Id);
			Assert.Equal(1000, target.CurrentHullPoints);
		}
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
		var shieldEffect = new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { target.Position }, 100, world.StateOf("enemy").Position, shieldDamageBonus: 50);

		shieldEffect.Apply(world, runtime, "enemy");

		Assert.Equal(80, Enum.GetValues<ESpatialOrientation>().Min(face => target.CurrentShieldPoints[face]));
		shieldEffect.Undo(world, runtime, "enemy");
		target.CurrentShieldPoints.Fill(0);
		target.CurrentHullPoints = 1000;
		target.Statuses.Add(BattlePassiveStatusInstaller.Create(EPilotCapabilityId.ReinforcedHull, target.Id));
		new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { target.Position }, 100, world.StateOf("enemy").Position)
			.Apply(world, runtime, "enemy");
		Assert.Equal(946, target.CurrentHullPoints);
	}

	[Fact]
	public void ReinstallForkAndSaveLoadPreserveResistanceWithoutActivationWork()
	{
		using var battle = Battle();
		var runtime = battle.Engine.ActorRuntimes.For("player");
		var world = battle.Engine.World;
		BattlePassiveStatusInstaller.InstallFromCapabilities(world, "player", runtime);
		Assert.Single(world.StateOf("player").Statuses);
		Assert.Empty(world.ActivationRoundLists.EntriesFor("player"));
		var registry = PersistenceRegistry.CreateDefault();
		var restored = SaveDtoMapper.RestoreBattleWorld(SaveDtoMapper.CaptureBattleWorld(world, registry), registry);
		var unit = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(world.StateOf("player")));
		foreach (var state in new[] { restored.StateOf("player"), world.Fork().StateOf("player"), unit })
			Assert.NotNull(Assert.Single(state.Statuses).RewriteIncomingHit);
		var hit = BattleStatuses.RewriteIncomingHit(restored.StateOf("player").Statuses, restored,
			restored.UnitRegistry.UnitOf("player"), restored.StateOf("enemy").Position, EImpactCause.LightningCannonBurst,
			ESpatialOrientation.Forward, new IncomingHit(100, 50));
		Assert.Equal(new IncomingHit(80, 40), hit);
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
