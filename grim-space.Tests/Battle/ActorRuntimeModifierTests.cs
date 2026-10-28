using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

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
	public void NoModifiers_PassThroughEffects()
	{
		var runtime = ActorRuntime.From(null);
		var battle = CreateBattle(runtime);
		var effects = SampleEffects();

		var modified = runtime.ActionEffects.Apply(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			effects);

		Assert.Equal(effects, modified);
	}

	[Fact]
	public void LightningTag_AppliesIonizedShieldBonus()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var effects = SampleEffects();

		var modified = runtime.ActionEffects.Apply(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			effects);

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			((ApplyAreaDamageEffect)modified[0]).ShieldDamageBonusPercent);
		Assert.IsType<MountUsesChangeEffect>(modified[1]);
	}

	[Fact]
	public void DodgeThis_AddsAdjacentHitDamagePercent()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.DodgeThis]));
		var battle = CreateBattle(runtime);

		var modified = runtime.ActionEffects.Apply(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			SampleEffects());

		Assert.Equal(
			PilotSkillPercents.DodgeThis.BonusHitDamagePercent,
			((ApplyAreaDamageEffect)modified[0]).AdjacentHitDamagePercent);
		Assert.Equal(
			PilotSkillPercents.DodgeThis.BonusRangeCells,
			((ApplyAreaDamageEffect)modified[0]).AdjacentHitDamageRangeCells);
		Assert.IsType<MountUsesChangeEffect>(modified[1]);
	}

	[Fact]
	public void DodgeThisAndIonizedPayload_ComposeBothDamageBonuses()
	{
		var runtime = ActorRuntime.From(PilotBattleSnapshot.FromSkills(
			"pilot",
			[EPilotSkillId.DodgeThis, EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);

		var modified = runtime.ActionEffects.Apply(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			SampleEffects());

		var damage = Assert.IsType<ApplyAreaDamageEffect>(modified[0]);
		Assert.Equal(PilotSkillPercents.DodgeThis.BonusHitDamagePercent, damage.AdjacentHitDamagePercent);
		Assert.Equal(PilotSkillPercents.DodgeThis.BonusRangeCells, damage.AdjacentHitDamageRangeCells);
		Assert.Equal(PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent, damage.ShieldDamageBonusPercent);
	}

	[Fact]
	public void MissingLightningTag_SkipsIonizedModifier()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var effects = SampleEffects();

		var modified = runtime.ActionEffects.Apply(
			new MoveStepAction("player"),
			battle.Engine.World,
			runtime,
			effects);

		Assert.Equal(0, ((ApplyAreaDamageEffect)modified[0]).ShieldDamageBonusPercent);
	}

	[Fact]
	public void TaggedLightningAttack_AppliesIonizedShieldBonusWithoutLightningCannonAction()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var areaDamage = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(),
			2,
			Coord.Zero);

		var modified = runtime.ActionEffects.Apply(
			new TaggedLightningAttackAction("player"),
			battle.Engine.World,
			runtime,
			[areaDamage]);

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			Assert.IsType<ApplyAreaDamageEffect>(modified[0]).ShieldDamageBonusPercent);
	}

	[Fact]
	public void AttackOnlyTaggedAction_SkipsIonizedShieldBonusOnAreaDamage()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var areaDamage = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(),
			2,
			Coord.Zero);

		var modified = runtime.ActionEffects.Apply(
			new AttackOnlyTaggedAction("player"),
			battle.Engine.World,
			runtime,
			[areaDamage]);

		Assert.Equal(0, Assert.IsType<ApplyAreaDamageEffect>(modified[0]).ShieldDamageBonusPercent);
	}

	[Fact]
	public void NonAreaEffect_UnchangedByIonizedModifier()
	{
		var runtime = ActorRuntime.From(
			PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var mount = new GrimSpace.Units.Loadouts.Abilities.AbilityMount(
			EAbilityKind.LightningCannon,
			ESpatialOrientation.Forward);
		var effects = new GrimSpace.Core.Actions.IEffect<GrimSpace.Battle.World.BattleWorld, ActorRuntime>[]
		{
			new MountUsesChangeEffect(mount, -1),
		};

		var modified = runtime.ActionEffects.Apply(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			effects);

		Assert.IsType<MountUsesChangeEffect>(modified[0]);
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

	private static GrimSpace.Core.Actions.IEffect<GrimSpace.Battle.World.BattleWorld, ActorRuntime>[] SampleEffects() =>
	[
		new ApplyAreaDamageEffect(EImpactCause.LightningCannonBurst, new HashSet<Coord>(), 2, Coord.Zero),
		new MountUsesChangeEffect(
			new GrimSpace.Units.Loadouts.Abilities.AbilityMount(
				EAbilityKind.LightningCannon,
				ESpatialOrientation.Forward),
			-1),
	];
}
