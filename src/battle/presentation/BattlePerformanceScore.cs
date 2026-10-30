namespace GrimSpace.Battle.Presentation;

public static class BattlePerformanceScore
{
	public static int Points(int damageDealt, int damageTaken, int healingDone = 0) =>
		damageDealt + healingDone - damageTaken;

	public static char Grade(int damageDealt, int damageTaken, int healingDone = 0)
	{
		var points = Points(damageDealt, damageTaken, healingDone);
		return points switch
		{
			>= 80 => 'S',
			>= 40 => 'A',
			>= 10 => 'B',
			_ => 'C',
		};
	}
}
