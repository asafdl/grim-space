using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
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

public sealed class ScrapDroneSwarmDamageBonusTests
{
	[Fact]
	public void StatusPass_AddsOneDamageToTaggedScrapDroneAttack()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			player.State,
			ActorRuntime.From(new GrimSpace.Battle.Encounter.PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.ScrapDrone,
					BattlePilotCapabilityPipelineFactory.Supports))));
		var world = BattleWorldFor(player);
		IAction action = new ScrapDroneSwarmAction(player.State.Id, ESpatialOrientation.Port);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { new(1, 1, 0) },
			3 * 100,
			new Coord(0, 0, 0));

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(4 * 100, modified.Damage);
		Assert.Contains(BattleActionTags.Attack, action.Tags);
		Assert.Contains(BattleActionTags.ScrapDrone, action.Tags);
	}

	[Fact]
	public void StatusPass_DamageBonusDoesNotIncreaseHealAmount()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			player.State,
			ActorRuntime.From(new GrimSpace.Battle.Encounter.PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.ScrapDrone,
					BattlePilotCapabilityPipelineFactory.Supports))));
		var world = BattleWorldFor(player);
		IAction action = new ScrapDroneSwarmAction(player.State.Id, ESpatialOrientation.Port);
		IEffect<BattleWorld, ActorRuntime>[] effects =
		[
			new ApplyAreaDamageEffect(
				EImpactCause.ScrapDroneSwarmBurst,
				new HashSet<Coord> { new(1, 1, 0) },
				3 * 100,
				new Coord(0, 0, 0)),
			new ApplyAreaAllyHealEffect(
				EImpactCause.ScrapDroneSwarmBurst,
				new HashSet<Coord> { new(1, 1, 0) },
				3 * 100),
		];

		var modified = BattleStatuses.RewriteOutgoing(action, world, effects);

		Assert.Equal(4 * 100, Assert.IsType<ApplyAreaDamageEffect>(modified[0]).Damage);
		Assert.Equal(3 * 100, Assert.IsType<ApplyAreaAllyHealEffect>(modified[1]).HealAmount);
	}

	[Fact]
	public void Pipeline_NoLongerRegistersScrapDroneDamageBonus()
	{
		var pipeline = BattlePilotCapabilityPipelineFactory.Build(
			new HashSet<EPilotCapabilityId>
			{
				EPilotCapabilityId.ScrapDroneSwarmDamageBonus,
			});
		IAction action = new ScrapDroneSwarmAction("pilot-ship", ESpatialOrientation.Port);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { new(1, 1, 0) },
			3 * 100,
			new Coord(0, 0, 0));

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(pipeline.Apply(action, null!, ActorRuntime.From(null), [effect])));

		Assert.Equal(3 * 100, modified.Damage);
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
