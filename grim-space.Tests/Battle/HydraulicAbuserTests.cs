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

public sealed class HydraulicAbuserTests
{
	[Fact]
	public void StatusPass_ReducesIncomingHitDamageMultiplicatively()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var enemy = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var world = BattleWorldFor(player, enemy);
		InstallHydraulicAbuser(world, player);
		IAction action = new HighVelocityRodAction(enemy.State.Id, ESpatialOrientation.Forward);
		var effect = new DamageUnitEffect(
			EImpactCause.HighVelocityRod,
			player.State.Id,
			enemy.State.Position,
			100);

		var modified = BattleEffectTestKit.PrepareDamage(
			action,
			world,
			new ActorRuntime(),
			effect);

		Assert.Equal(
			[new HitStep(PilotSkillPercents.HydraulicAbuser.DamageTakenPercent)],
			modified.HitSteps);
	}

	[Fact]
	public void StatusPass_StacksDamageResistanceMultiplicatively()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var enemy = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var world = BattleWorldFor(player, enemy);
		player.State.Statuses.Add(
			BattleStatusMaterializer.Create(EPilotCapabilityId.DamageResistance));
		player.State.Statuses.Add(
			BattleStatusMaterializer.Create(EPilotCapabilityId.DamageResistance));
		IAction action = new HighVelocityRodAction(enemy.State.Id, ESpatialOrientation.Forward);
		var effect = new DamageUnitEffect(
			EImpactCause.HighVelocityRod,
			player.State.Id,
			enemy.State.Position,
			100);

		var modified = BattleEffectTestKit.PrepareDamage(
			action,
			world,
			new ActorRuntime(),
			effect);

		Assert.Equal(
			[
				new HitStep(PilotSkillPercents.HydraulicAbuser.DamageTakenPercent),
				new HitStep(PilotSkillPercents.HydraulicAbuser.DamageTakenPercent),
			],
			modified.HitSteps);
	}

	private static void InstallHydraulicAbuser(BattleWorld world, Unit player) =>
		BattleStatusMaterializer.Materialize(
			world,
			player.State.Id,
			ActorRuntime.From(new PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.HydraulicAbuser,
					BattlePilotCapabilities.Supports))));

	private static BattleWorld BattleWorldFor(params Unit[] units) =>
		BattleWorld.FromLive(
			units,
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"hydraulic-abuser-test",
			EObjective.EliminateOpponents,
			units.Select(unit => unit.State.Id).ToHashSet(StringComparer.Ordinal),
			new Timeline());
}
