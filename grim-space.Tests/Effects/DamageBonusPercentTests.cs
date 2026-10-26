using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class DamageBonusPercentTests
{
	[Fact]
	public void ApplyAreaDamage_AppliesDamageBonusPercent()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(6, 5, 5));
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			enemy.State.ShieldPoints[face] = 0;
		var hullBefore = enemy.State.HullPoints;
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { enemy.State.Position },
			10,
			player.State.Position)
			.WithDamageBonusPercent(80);

		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(player.State.Id),
			player.State.Id);

		Assert.Equal(hullBefore - 18, enemy.State.HullPoints);
	}
}
