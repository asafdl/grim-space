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

public sealed class AdjacentDamageBonusTests
{
	[Fact]
	public void StatusPass_StampsAdjacentBonusOnTaggedAttack()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var world = BattleWorldFor(player);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			world,
			player.State.Id,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.DodgeThis])));
		IAction action = new LightningCannonAction(player.State.Id);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { new(1, 0, 0) },
			2,
			Coord.Zero);

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(
			PilotSkillPercents.DodgeThis.BonusHitDamagePercent,
			modified.AdjacentHitDamagePercent);
		Assert.Equal(
			PilotSkillPercents.DodgeThis.BonusRangeCells,
			modified.AdjacentHitDamageRangeCells);
	}

	[Fact]
	public void StatusPass_LeavesNonAreaEffectsUnchanged()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var world = BattleWorldFor(player);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			world,
			player.State.Id,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.DodgeThis])));
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

		Assert.Equal(0, modified.DamageBonusPercent);
	}

	[Fact]
	public void Pipeline_NoLongerRegistersAdjacentDamageBonus()
	{
		var pipeline = BattlePilotCapabilityPipelineFactory.Build(
			new HashSet<EPilotCapabilityId>
			{
				EPilotCapabilityId.AdjacentDamageBonus,
			});
		IAction action = new LightningCannonAction("pilot-ship");
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(),
			2,
			Coord.Zero);

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(pipeline.Apply(action, null!, ActorRuntime.From(null), [effect])));

		Assert.Equal(100, modified.AdjacentHitDamagePercent);
		Assert.Equal(1, modified.AdjacentHitDamageRangeCells);
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
