using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ApplyAreaAllyHealEffectTests
{
	[Fact]
	public void Apply_RestoresHullWithoutDamagingAllies()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			origin,
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var ally = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			origin + new Coord(0, 1, 0),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(origin + Coord.Forward * 8);
		ally.State.HullPoints = 50;
		var hullBefore = ally.State.HullPoints;
		var battle = BattleTestFixture.BeginSimulation(player, [ally, enemy]);
		var cells = new HashSet<Coord> { ally.State.Position };
		var effect = new ApplyAreaAllyHealEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			cells,
			100);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		var records = effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(hullBefore + 100, ally.State.HullPoints);
		var heal = Assert.IsType<Record<HealFacts>>(Assert.Single(records));
		Assert.Equal(100, heal.Value.HullRestored);
		Assert.Equal(0, heal.Value.ShieldRestored);
	}

	[Fact]
	public void Apply_PrioritizesMostMissingShieldFace()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			origin,
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var ally = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			origin + new Coord(0, 1, 0),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(origin + Coord.Forward * 8);
		var battle = BattleTestFixture.BeginSimulation(player, [ally, enemy]);
		ally.State.ShieldPoints[ESpatialOrientation.Port] = 0;
		ally.State.ShieldPoints[ESpatialOrientation.Starboard] = 50;
		var cells = new HashSet<Coord> { ally.State.Position };
		var effect = new ApplyAreaAllyHealEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			cells,
			100);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(100, ally.State.ShieldPoints[ESpatialOrientation.Port]);
		Assert.Equal(50, ally.State.ShieldPoints[ESpatialOrientation.Starboard]);
	}

	[Fact]
	public void WithHealBonusPercent_IncreasesRestoredHull()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			origin,
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var ally = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			origin + new Coord(0, 1, 0),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[ally, BattleTestFixture.Enemy(origin + Coord.Forward * 8)]);
		ally.State.HullPoints = 50;
		var cells = new HashSet<Coord> { ally.State.Position };
		var effect = new ApplyAreaAllyHealEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			cells,
			100).WithHealBonusPercent(20);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(170, ally.State.HullPoints);
	}
}
