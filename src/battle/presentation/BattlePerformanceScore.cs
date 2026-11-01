namespace GrimSpace.Battle.Presentation;

public static class BattlePerformanceScore
{
	/// <summary>
	/// Raw hull/shield totals are hundreds per fight; divide into the grade band (~0–100)
	/// so S/A thresholds and XP stay meaningful.
	/// </summary>
	public const int CombatStatScale = 10;

	/// <summary>
	/// Rank contribution from output only. <paramref name="damageTaken"/> is unused for now
	/// (kept so call sites can keep passing scorecard totals).
	/// </summary>
	public static int Points(int damageDealt, int damageTaken, int healingDone = 0)
	{
		_ = damageTaken;
		var raw = damageDealt + healingDone;
		if (raw <= 0)
			return 0;

		return (int)System.Math.Round(
			raw / (decimal)CombatStatScale,
			System.MidpointRounding.AwayFromZero);
	}

	public static char Grade(int damageDealt, int damageTaken, int healingDone = 0) =>
		GradeFromPoints(Points(damageDealt, damageTaken, healingDone));

	public static char GradeFromPoints(int performancePoints) =>
		performancePoints switch
		{
			>= 80 => 'S',
			>= 40 => 'A',
			>= 10 => 'B',
			_ => 'C',
		};
}
