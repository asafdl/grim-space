using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Objectives;
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

public sealed class ReinforcedHullStatusTests
{
	[Fact]
	public void PrepareDamage_ScalesHullOnly()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var enemy = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		player.State.Statuses.Add(
			BattleStatusMaterializer.Create(EPilotCapabilityId.ReinforcedHull));
		var world = BattleWorldFor(player, enemy);
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
			[PilotSkillPercents.ReinforcedHull.HullDamageTakenPercent],
			modified.HullDamagePercents);
		Assert.Empty(modified.HitSteps);
	}

	[Fact]
	public void ApplyDamage_UsesHullChannel()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		player.State.CurrentShieldPoints[ESpatialOrientation.Forward] = 0;
		var hullBefore = player.State.CurrentHullPoints;

		Defense.ApplyDamage(
			player.State,
			9,
			ESpatialOrientation.Forward,
			0,
			[PilotSkillPercents.ReinforcedHull.HullDamageTakenPercent]);

		Assert.Equal(hullBefore - 6, player.State.CurrentHullPoints);
	}

	[Fact]
	public void ApplyDamage_ShieldChannelUnchangedByReinforcedHull()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		player.State.Statuses.Add(
			BattleStatusMaterializer.Create(EPilotCapabilityId.ReinforcedHull));
		player.State.CurrentShieldPoints[ESpatialOrientation.Forward] = 10;

		Defense.ApplyDamage(
			player.State,
			6,
			ESpatialOrientation.Forward,
			0,
			[PilotSkillPercents.ReinforcedHull.HullDamageTakenPercent]);

		Assert.Equal(4, player.State.CurrentShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(
			player.State.Loadout.BaseHullPoints,
			player.State.CurrentHullPoints);
	}

	private static BattleWorld BattleWorldFor(params Unit[] units) =>
		BattleWorld.FromLive(
			units,
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"reinforced-hull-status-test",
			EObjective.EliminateOpponents,
			units.Select(unit => unit.State.Id).ToHashSet(StringComparer.Ordinal),
			new Timeline());
}
