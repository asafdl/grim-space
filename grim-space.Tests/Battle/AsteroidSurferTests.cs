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
		var enemy = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var world = BattleWorldFor(player, enemy);
		var runtime = ActorRuntime.From(new PilotBattleSnapshot(
			"pilot",
			PilotCapabilityGrant.FromBackground(
				"pilot",
				EPilotBackgroundId.AsteroidSurfer,
				BattlePilotCapabilities.Supports)));
		BattleStatusMaterializer.Materialize(world, player.State.Id, runtime);
		IAction action = new HighVelocityRodAction(player.State.Id, ESpatialOrientation.Forward);
		var effect = new DamageUnitEffect(
			EImpactCause.HighVelocityRod,
			enemy.State.Id,
			Coord.Zero,
			100);

		var modified = BattleEffectTestKit.PrepareDamage(action, world, runtime, effect);

		Assert.Equal(
			PilotSkillPercents.AsteroidSurfer.OutgoingDamageBonusPercent,
			modified.BonusPercent);
	}

	[Fact]
	public void StatusPass_IncreasesHullDamageTakenOnly()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var enemy = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var world = BattleWorldFor(player, enemy);
		var runtime = ActorRuntime.From(new PilotBattleSnapshot(
			"pilot",
			PilotCapabilityGrant.FromBackground(
				"pilot",
				EPilotBackgroundId.AsteroidSurfer,
				BattlePilotCapabilities.Supports)));
		BattleStatusMaterializer.Materialize(world, player.State.Id, runtime);
		IAction action = new HighVelocityRodAction(enemy.State.Id, ESpatialOrientation.Forward);
		var effect = new DamageUnitEffect(
			EImpactCause.HighVelocityRod,
			player.State.Id,
			enemy.State.Position,
			100);

		var modified = BattleEffectTestKit.PrepareDamage(action, world, runtime, effect);

		Assert.Equal(
			[PilotSkillPercents.AsteroidSurfer.HullDamageTakenPercent],
			modified.HullDamagePercents);
		Assert.Empty(modified.HitSteps);
	}

	private static BattleWorld BattleWorldFor(params Unit[] units) =>
		BattleWorld.FromLive(
			units,
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"asteroid-surfer-test",
			EObjective.EliminateOpponents,
			units.Select(unit => unit.State.Id).ToHashSet(StringComparer.Ordinal),
			new Timeline());
}
