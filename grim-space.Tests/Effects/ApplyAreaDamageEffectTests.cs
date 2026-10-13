using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ApplyAreaDamageEffectTests
{
	[Fact]
	public void WithAdditionalShieldDamage_AddsOnlyToShieldBonus()
	{
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(),
			3,
			Coord.Zero,
			1);
		var adjusted = effect.WithAdditionalShieldDamage(2);

		Assert.Equal(3, adjusted.Damage);
		Assert.Equal(3, adjusted.ShieldDamageBonus);
	}

	[Fact]
	public void Apply_UsesShieldDamageBonusOnlyAgainstShields()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(6, 5, 5));
		var hitFace = GrimSpace.Battle.Spatial.BodyFrame.From(enemy.State).HitFaceFrom(player.State.Position);
		enemy.State.ShieldPoints[hitFace] = 2;
		var hullBefore = enemy.State.HullPoints;
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			2,
			player.State.Position,
			1);

		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(hullBefore, enemy.State.HullPoints);
		Assert.Equal(0, enemy.State.ShieldPoints[hitFace]);

		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(2, enemy.State.ShieldPoints[hitFace]);
	}
}
