namespace GrimSpace.Battle.Presentation.Ui;

public sealed record BattleDamageReportRow(string TargetLabel, int ExperienceGained);

public sealed record BattleVictorySummary(
	IReadOnlyList<BattleDamageReportRow> DamageRows,
	BattleDamageReportRow? CompletionRow);
