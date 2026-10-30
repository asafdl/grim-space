using GrimSpace.Battle.Presentation;

namespace GrimSpace.Tests.Presentation;

public sealed class BattlePerformanceScoreTests
{
	[Theory]
	[InlineData(0, 0, 0, 0, 'C')]
	[InlineData(50, 0, 0, 5, 'C')]
	[InlineData(100, 0, 0, 10, 'B')]
	[InlineData(400, 0, 0, 40, 'A')]
	[InlineData(800, 0, 0, 80, 'S')]
	[InlineData(400, 100, 0, 30, 'B')]
	public void PointsAndGrade_ScaleCombatStatsIntoBand(
		int dealt,
		int taken,
		int healing,
		int expectedPoints,
		char expectedGrade)
	{
		Assert.Equal(expectedPoints, BattlePerformanceScore.Points(dealt, taken, healing));
		Assert.Equal(expectedGrade, BattlePerformanceScore.Grade(dealt, taken, healing));
	}
}
