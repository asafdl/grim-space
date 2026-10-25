namespace GrimSpace.Battle.Presentation;

public static class BattlePerformanceScore
{
	public static int Points(int damageDealt, int damageTaken) =>
		damageDealt * 2 - damageTaken;

	public static char Grade(int damageDealt, int damageTaken)
	{
		var points = Points(damageDealt, damageTaken);
		return points switch
		{
			>= 80 => 'S',
			>= 40 => 'A',
			>= 10 => 'B',
			_ => 'C',
		};
	}
}
