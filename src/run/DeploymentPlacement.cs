using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Units;

namespace GrimSpace.Run;

public static class DeploymentPlacement
{
	private const int Margin = 4;
	private const int DeploySpreadDivisor = 5;
	private const int EnemySpreadBand = 4;
	private const int LaneHalfBand = 8;

	public static (Spawn Player, Spawn Enemy) DevDuel(
		Instance playerInstance,
		Instance enemyInstance,
		int seed,
		int gridSize,
		int playerMomentum = 0,
		int enemyMomentum = 2)
	{
		var center = gridSize / 2;
		var deploySpread = gridSize / DeploySpreadDivisor;
		var playerPosition = new Coord(center - deploySpread, center, center);
		var enemyPosition = PickEnemyPosition(seed, gridSize, playerPosition);
		var dorsal = Coord.Up;
		var playerFore = AxisToward(playerPosition, enemyPosition);
		var enemyFore = AxisToward(enemyPosition, playerPosition);

		return (
			new Spawn
			{
				Unit = playerInstance,
				Position = playerPosition,
				InitialMomentum = playerMomentum,
				Fore = playerFore,
				Dorsal = dorsal,
				ExecutionAgent = new UserExecutionAgent(),
			},
			new Spawn
			{
				Unit = enemyInstance,
				Position = enemyPosition,
				InitialMomentum = enemyMomentum,
				Fore = enemyFore,
				Dorsal = dorsal,
				ExecutionAgent = new AiController(),
			});
	}

	private static Coord PickEnemyPosition(int seed, int gridSize, Coord playerPosition)
	{
		var rng = new Random(seed);
		var half = gridSize / 2;
		var center = gridSize / 2;

		var deploySpread = gridSize / DeploySpreadDivisor;
		var enemyCenter = center + deploySpread;
		var minX = playerPosition.X < half
			? enemyCenter - EnemySpreadBand
			: Margin;
		var maxX = playerPosition.X < half
			? enemyCenter + EnemySpreadBand
			: half - Margin - 1;

		return new Coord(
			rng.Next(minX, maxX + 1),
			rng.Next(center - LaneHalfBand, center + LaneHalfBand + 1),
			rng.Next(center - LaneHalfBand, center + LaneHalfBand + 1));
	}

	private static Coord AxisToward(Coord from, Coord to)
	{
		var delta = to - from;
		var ax = System.Math.Abs(delta.X);
		var ay = System.Math.Abs(delta.Y);
		var az = System.Math.Abs(delta.Z);

		if (ax >= ay && ax >= az)
			return new Coord(System.Math.Sign(delta.X), 0, 0);

		if (ay >= ax && ay >= az)
			return new Coord(0, System.Math.Sign(delta.Y), 0);

		return new Coord(0, 0, System.Math.Sign(delta.Z));
	}
}
