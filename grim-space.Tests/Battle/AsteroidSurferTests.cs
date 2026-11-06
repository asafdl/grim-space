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

namespace GrimSpace.Tests.PilotCapabilities;

public sealed class AsteroidSurferTests
{
	[Fact]
	public void StatusPass_AddsOutgoingDamageBonusPercent()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var world = BattleWorldFor(player);
		InstallAsteroidSurfer(world, player);
		IAction action = new ScrapDroneSwarmAction(player.State.Id, ESpatialOrientation.Port);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { new(1, 1, 0) },
			10 * 100,
			Coord.Zero);

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(
			PilotSkillPercents.AsteroidSurfer.OutgoingDamageBonusPercent,
			modified.DamageBonusPercent);
	}

	[Fact]
	public void StatusPass_IncreasesHullDamageTakenOnly()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var world = BattleWorldFor(player);
		InstallAsteroidSurfer(world, player);

		Assert.Equal(
			115,
			BattleStatuses.RewriteHullDamage(player.State.Statuses, 100));
		Assert.Equal(
			100,
			BattleStatuses.RewriteShieldDamage(player.State.Statuses, 100));
	}

	private static void InstallAsteroidSurfer(BattleWorld world, Unit player) =>
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			world,
			player.State.Id,
			ActorRuntime.From(new PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.AsteroidSurfer,
					BattlePilotCapabilityPipelineFactory.Supports))));

	private static BattleWorld BattleWorldFor(Unit player) =>
		BattleWorld.FromLive(
			[player],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"asteroid-surfer-test",
			EObjective.EliminateOpponents,
			new HashSet<string> { player.State.Id },
			new Timeline());
}
