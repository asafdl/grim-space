using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.BattleRuntime;

[BattleTestSuite]
public sealed class ActorRuntimeModifierTests
{
	private sealed record TaggedLightningAttackAction(string ActorId) : IAction
	{
		private static readonly string[] ActionTags =
			[BattleActionTags.Attack, BattleActionTags.Lightning];

		IReadOnlyList<string> IAction.Tags => ActionTags;
	}

	private sealed record AttackOnlyTaggedAction(string ActorId) : IAction
	{
		private static readonly string[] ActionTags = [BattleActionTags.Attack];

		IReadOnlyList<string> IAction.Tags => ActionTags;
	}

	[Fact]
	public void LightningTag_AppliesIonizedShieldBonus()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var prepared = BattleEffectTestKit.PrepareDamage(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			SampleDamage(battle));

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			prepared.ShieldDamageBonusPercent);
	}

	[Fact]
	public void DodgeThis_AddsAdjacentHitDamagePercent()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.DodgeThis]));
		var battle = CreateBattle(runtime);
		var enemy = battle.Engine.World.UnitRegistry.All.First(unit => unit.State.Id != "player");
		enemy.State.Position = battle.Engine.World.StateOf("player").Position
			+ new Coord(0, PilotSkillPercents.DodgeThis.BonusRangeCells, 0);
		var prepared = BattleEffectTestKit.PrepareDamage(
			new HighVelocityRodAction("player", ESpatialOrientation.Forward),
			battle.Engine.World,
			runtime,
			new DamageUnitEffect(
				EImpactCause.HighVelocityRod,
				enemy.State.Id,
				battle.Engine.World.StateOf("player").Position,
				100));

		Assert.Equal(
			[PilotSkillPercents.DodgeThis.BonusHitDamagePercent],
			prepared.DamagePercents);
	}

	[Fact]
	public void DodgeThisAndIonizedPayload_ComposeBothDamageBonuses()
	{
		var runtime = ActorRuntime.From(PilotBattleSnapshot.FromSkills(
			"pilot",
			[EPilotSkillId.DodgeThis, EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var prepared = BattleEffectTestKit.PrepareDamage(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			SampleDamage(battle));

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			prepared.ShieldDamageBonusPercent);
		Assert.Empty(prepared.DamagePercents);
	}

	[Fact]
	public void MissingLightningTag_SkipsIonizedModifier()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var prepared = BattleEffectTestKit.PrepareDamage(
			new MoveStepAction("player"),
			battle.Engine.World,
			runtime,
			SampleDamage(battle));

		Assert.Equal(0, prepared.ShieldDamageBonusPercent);
	}

	[Fact]
	public void TaggedLightningAttack_AppliesIonizedShieldBonusWithoutLightningCannonAction()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var prepared = BattleEffectTestKit.PrepareDamage(
			new TaggedLightningAttackAction("player"),
			battle.Engine.World,
			runtime,
			SampleDamage(battle));

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			prepared.ShieldDamageBonusPercent);
	}

	[Fact]
	public void AttackOnlyTaggedAction_SkipsIonizedShieldBonusOnAreaDamage()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var prepared = BattleEffectTestKit.PrepareDamage(
			new AttackOnlyTaggedAction("player"),
			battle.Engine.World,
			runtime,
			SampleDamage(battle));

		Assert.Equal(0, prepared.ShieldDamageBonusPercent);
	}

	private static GrimSpace.Battle.BattleOrchestrator CreateBattle(ActorRuntime playerRuntime)
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("player", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = new PilotBattleSnapshot(
				"pilot",
				playerRuntime.PilotCapabilityGrants),
		};
		return BattleTestFixture.BeginSimulation(player, enemy, pilotsByActorId: pilots);
	}

	private static DamageUnitEffect SampleDamage(GrimSpace.Battle.BattleOrchestrator battle)
	{
		var enemy = battle.Engine.World.UnitRegistry.All.First(unit => unit.State.Id != "player");
		return new DamageUnitEffect(
			EImpactCause.LightningCannonBurst,
			enemy.State.Id,
			Coord.Zero,
			2);
	}
}
