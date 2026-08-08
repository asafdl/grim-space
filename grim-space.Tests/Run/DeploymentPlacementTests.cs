using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Run;

public sealed class DeploymentPlacementTests
{
	[Fact]
	public void DevDuel_PlacesPlayerOnLowXAndEnemyOnHighX()
	{
		var (player, enemy) = DeploymentPlacement.DevDuel(
			PlayerInstance(),
			EnemyInstance(),
			seed: 42,
			gridSize: 64);

		Assert.True(player.Position.X < 64 / 2);
		Assert.True(enemy.Position.X >= 64 / 2);
		Assert.NotEqual(player.Position, enemy.Position);
	}

	[Fact]
	public void DevDuel_PlayerNotAtGridCenter()
	{
		var (player, _) = DeploymentPlacement.DevDuel(
			PlayerInstance(),
			EnemyInstance(),
			seed: 42,
			gridSize: 64);

		Assert.NotEqual(new Coord(32, 32, 32), player.Position);
	}

	[Fact]
	public void DevDuel_UnitsFaceEachOther()
	{
		var (player, enemy) = DeploymentPlacement.DevDuel(
			PlayerInstance(),
			EnemyInstance(),
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
			PlayerInstance(), EnemyInstance(), seed: 1, gridSize: 64);
		var (_, enemyB) = DeploymentPlacement.DevDuel(
			PlayerInstance(), EnemyInstance(), seed: 2, gridSize: 64);

		Assert.NotEqual(enemyA.Position, enemyB.Position);
	}

	[Fact]
	public void DevDefault_UsesDeploymentPlacement()
	{
		var encounter = Encounter.DevDefault(seed: 99, gridSize: 64);
		var player = encounter.Spawns.First(spawn => spawn.Unit.Alliance.Team == ETeam.Player);
		var enemy = encounter.Spawns.First(spawn => spawn.Unit.Alliance.Team == ETeam.Enemy);

		Assert.True(player.Position.X < enemy.Position.X);
		Assert.NotEqual(Coord.Forward, player.Fore);
	}

	private static Instance PlayerInstance() => new()
	{
		Id = "player",
		Type = EType.Fighter,
		Alliance = Alliance.Player,
	};

	private static Instance EnemyInstance() => new()
	{
		Id = "enemy",
		Type = EType.Patrol,
		Alliance = Alliance.Enemy,
	};
}
