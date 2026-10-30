namespace GrimSpace.Battle.Presentation;

public static class BattlePerformanceScore
{
	/// <summary>
	/// Raw hull/shield totals are hundreds per fight; divide into the grade band (~0–100)
	/// so S/A thresholds and XP stay meaningful.
	/// </summary>
	public const int CombatStatScale = 10;

	public static int Points(int damageDealt, int damageTaken, int healingDone = 0)
	{
		var raw = damageDealt + healingDone - damageTaken;
		if (raw <= 0)
			return 0;

		return (int)System.Math.Round(
			raw / (decimal)CombatStatScale,
			System.MidpointRounding.AwayFromZero);
	}

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
