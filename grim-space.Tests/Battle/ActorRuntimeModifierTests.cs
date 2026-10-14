using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.BattleRuntime;

[BattleTestSuite]
public sealed class ActorRuntimeModifierTests
{
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
		var runtime = ActorRuntime.From(new PilotBattleSnapshot("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var effects = SampleEffects();

		var modified = runtime.ActionEffects.Apply(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			effects);

		Assert.Equal(1, ((ApplyAreaDamageEffect)modified[0]).ShieldDamageBonus);
		Assert.IsType<MountUsesChangeEffect>(modified[1]);
	}

	[Fact]
	public void DodgeThis_AddsAdjacentTargetDamageBonus()
	{
		var runtime = ActorRuntime.From(new PilotBattleSnapshot("pilot", [EPilotSkillId.DodgeThis]));
		var battle = CreateBattle(runtime);

		var modified = runtime.ActionEffects.Apply(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			SampleEffects());

		Assert.Equal(2, ((ApplyAreaDamageEffect)modified[0]).AdjacentTargetDamageBonus);
		Assert.IsType<MountUsesChangeEffect>(modified[1]);
	}

	[Fact]
	public void DodgeThisAndIonizedPayload_ComposeBothDamageBonuses()
	{
		var runtime = ActorRuntime.From(new PilotBattleSnapshot(
			"pilot",
			[EPilotSkillId.DodgeThis, EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);

		var modified = runtime.ActionEffects.Apply(
			new LightningCannonAction("player"),
			battle.Engine.World,
			runtime,
			SampleEffects());

		var damage = Assert.IsType<ApplyAreaDamageEffect>(modified[0]);
		Assert.Equal(2, damage.AdjacentTargetDamageBonus);
		Assert.Equal(1, damage.ShieldDamageBonus);
	}

	[Fact]
	public void MissingLightningTag_SkipsIonizedModifier()
	{
		var runtime = ActorRuntime.From(new PilotBattleSnapshot("pilot", [EPilotSkillId.IonizedPayload]));
		var battle = CreateBattle(runtime);
		var effects = SampleEffects();

		var modified = runtime.ActionEffects.Apply(
			new MoveStepAction("player"),
			battle.Engine.World,
			runtime,
			effects);

		Assert.Equal(0, ((ApplyAreaDamageEffect)modified[0]).ShieldDamageBonus);
	}

	[Fact]
	public void NonAreaEffect_UnchangedByIonizedModifier()
	{
		var runtime = ActorRuntime.From(new PilotBattleSnapshot("pilot", [EPilotSkillId.IonizedPayload]));
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
			[player.State.Id] = new PilotBattleSnapshot("pilot", playerRuntime.PilotSkills.ToArray()),
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
