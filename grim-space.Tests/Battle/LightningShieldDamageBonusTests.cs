using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.StatusModel;

public sealed class LightningShieldDamageBonusTests
{
	[Fact]
	public void StatusPass_StampsShieldBonusOnTaggedLightningAttack()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			player.State,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload])));
		var world = BattleWorldFor(player);
		IAction action = new LightningCannonAction(player.State.Id);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { new(1, 0, 0) },
			2,
			Coord.Zero);

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			modified.ShieldDamageBonusPercent);
	}

	[Fact]
	public void StatusPass_StampsShieldBonusOnChainLightningEffect()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			player.State,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload])));
		var world = BattleWorldFor(player);
		IAction action = new LightningCannonAction(player.State.Id);
		var effect = new ApplyChainLightningDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(),
			[],
			2,
			0,
			Coord.Zero);

		var modified = Assert.IsType<ApplyChainLightningDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			modified.ShieldDamageBonusPercent);
	}

	[Fact]
	public void Pipeline_NoLongerRegistersLightningShieldBonus()
	{
		var pipeline = BattlePilotCapabilityPipelineFactory.Build(
			new HashSet<EPilotCapabilityId>
			{
				EPilotCapabilityId.LightningShieldDamageBonus,
			});
		IAction action = new LightningCannonAction("pilot-ship");
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(),
			2,
			Coord.Zero);

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(pipeline.Apply(action, null!, ActorRuntime.From(null), [effect])));

		Assert.Equal(0, modified.ShieldDamageBonusPercent);
	}

	private static BattleWorld BattleWorldFor(Unit player) =>
		BattleWorld.FromLive(
			[player],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"status-test",
			EObjective.EliminateOpponents,
			new HashSet<string> { player.State.Id },
			new Timeline());
}
