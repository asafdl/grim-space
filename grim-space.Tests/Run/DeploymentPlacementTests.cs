using GrimSpace.Math.Grid;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class DeploymentPlacementTests
{
	[Fact]
	public void DevDuel_PlacesPlayerOnLowXAndEnemyOnHighX()
	{
		var (player, enemy) = DeploymentPlacement.DevDuel(
			EType.Fighter,
			EType.Carrier,
			seed: 42,
			gridSize: 64);

		Assert.True(player.Position.X < 64 / 2);
		Assert.True(enemy.Position.X >= 64 / 2);
		Assert.NotEqual(player.Position, enemy.Position);
	}

	[Fact]
	public void DevDuel_ExpandsMountsAndUpgradesEverySupportedWeapon()
	{
		var (player, enemy) = DeploymentPlacement.DevDuel(
			EType.Fighter,
			EType.Carrier,
			seed: 42,
			gridSize: 64);

		var playerCannon = player.Ship.Loadout.InstalledAbilities.Single(
			ability => ability.Kind == EAbilityKind.LightningCannon);
		var effective = Assert.IsType<LightningCannonSpec>(playerCannon.Spec);

		Assert.Equal(FighterSpec.Instance.Slots.Count, player.Ship.Loadout.InstalledAbilities.Count);
		Assert.All(
			player.Ship.Loadout.InstalledAbilities.Where(
				ability => ability.Spec.MaxDamageUpgrades > 0),
			ability => Assert.Equal(1, ability.DamageUpgradeTier));
		Assert.All(
			player.Ship.Loadout.InstalledAbilities.Where(
				ability => ability.Spec.MaxRangeUpgrades > 0),
			ability => Assert.Equal(1, ability.RangeUpgradeTier));
		Assert.Equal(LightningCannonSpec.Baseline.Damage + 1, effective.Damage);
		Assert.Equal(LightningCannonSpec.Baseline.LineLength + 1, effective.LineLength);
		Assert.All(
			enemy.Ship.Loadout.InstalledAbilities.Where(
				ability => ability.Spec.MaxDamageUpgrades > 0),
			ability => Assert.Equal(1, ability.DamageUpgradeTier));
		Assert.All(
			enemy.Ship.Loadout.InstalledAbilities.Where(
				ability => ability.Spec.MaxRangeUpgrades > 0),
			ability => Assert.Equal(1, ability.RangeUpgradeTier));
	}

	[Fact]
	public void DevDuel_PlayerNotAtGridCenter()
	{
		var (player, _) = DeploymentPlacement.DevDuel(
			EType.Fighter,
			EType.Carrier,
			seed: 42,
			gridSize: 64);

		Assert.NotEqual(new Coord(32, 32, 32), player.Position);
	}

	[Fact]
	public void DevDuel_UnitsFaceEachOther()
	{
		var (player, enemy) = DeploymentPlacement.DevDuel(
			EType.Fighter,
			EType.Carrier,
			seed: 42,
			gridSize: 64);

		var delta = enemy.Position - player.Position;
		Assert.Equal(System.Math.Sign(delta.X), player.Fore.X);
		Assert.Equal(System.Math.Sign(-delta.X), enemy.Fore.X);
	}

	[Fact]
	public void DevDuel_EnemyPositionVariesBySeed()
	{
		var (_, enemyA) = DeploymentPlacement.DevDuel(
			EType.Fighter, EType.Carrier, seed: 1, gridSize: 64);
		var (_, enemyB) = DeploymentPlacement.DevDuel(
			EType.Fighter, EType.Carrier, seed: 2, gridSize: 64);

		Assert.NotEqual(enemyA.Position, enemyB.Position);
	}

	[Fact]
	public void DevDuel_KeepsEnemyInsideSmallGrid()
	{
		var (_, enemy) = DeploymentPlacement.DevDuel(
			EType.Fighter, EType.Carrier, seed: 8, gridSize: 16);

		Assert.InRange(enemy.Position.X, 0, 15);
		Assert.InRange(enemy.Position.Y, 0, 15);
		Assert.InRange(enemy.Position.Z, 0, 15);
	}

	[Fact]
	public void DevDefault_UsesDeploymentPlacement()
	{
		var encounter = BattleEncounter.DevDefault(seed: 99, gridSize: 64);
		var player = encounter.Spawns.First(spawn => spawn.Team == ETeam.Player);
		var enemy = encounter.Spawns.First(spawn => spawn.Team == ETeam.Enemy);

		Assert.True(player.Position.X < enemy.Position.X);
		Assert.NotEqual(Coord.Forward, player.Fore);
	}

	[Fact]
	public void DevDefault_UpgradesGunshipAndIndustrialGooperWeapons()
	{
		var encounter = BattleEncounter.DevDefault(seed: 99, gridSize: 64);
		var ships = encounter.Spawns.Select(spawn => spawn.Ship).ToArray();
		var gunship = ships.Single(ship => ship.Spec.Chassis == EType.Gunship);
		var gooper = ships.Single(ship => ship.Spec.Chassis == EType.IndustrialGooper);

		Assert.All(
			gunship.Loadout.InstalledAbilities,
			ability =>
			{
				Assert.Equal(1, ability.DamageUpgradeTier);
				Assert.Equal(1, ability.RangeUpgradeTier);
			});
		var swarm = gooper.Loadout.InstalledAbilities.Single(
			ability => ability.Kind == EAbilityKind.ScrapDroneSwarm);
		Assert.Equal(1, swarm.DamageUpgradeTier);
		Assert.Equal(1, swarm.RangeUpgradeTier);
	}

	[Fact]
	public void DevDefault_AddsIndustrialGooperBesideEnemy()
	{
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed, gridSize: 64);
			var carrier = encounter.Spawns.Single(spawn => spawn.Ship.Spec.Chassis == EType.Carrier);
			var gooper = encounter.Spawns.Single(spawn => spawn.Ship.Spec.Chassis == EType.IndustrialGooper);

			Assert.Equal(ETeam.Enemy, gooper.Team);
			Assert.Equal(4, carrier.Position.ManhattanDistanceTo(gooper.Position));
			Assert.Equal(carrier.Fore, gooper.Fore);
			Assert.Equal(carrier.Dorsal, gooper.Dorsal);
		}
	}

	[Fact]
	public void FromEncounter_AssignsGeneratedUnitIds()
	{
		var battle = GrimSpace.Battle.BattleOrchestrator.FromEncounter(
			BattleEncounter.DevDefault(seed: 7, gridSize: 32),
			gridSize: 32);

		Assert.Equal("fighter-dev-7", battle.PlayerId);
		Assert.Equal("carrier-dev-7", BattleTestFixture.FirstEnemyId(battle));
	}

}
