namespace GrimSpace.Battle.Presentation.Ui;

public sealed record BattleVictoryPilotRow(
	string DisplayName,
	string PortraitId,
	string ShipLine,
	int DamageDealt,
	int DamageTaken,
	int HealingDone,
	int ExperienceEarned,
	int ExperiencePointsBefore,
	int ExperiencePointsAfter,
	char ScoreGrade);

public sealed record BattleVictorySummary(IReadOnlyList<BattleVictoryPilotRow> Pilots);
