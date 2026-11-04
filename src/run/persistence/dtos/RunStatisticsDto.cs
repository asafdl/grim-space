namespace GrimSpace.Run.Persistence;

public sealed record RunStatisticsDto(
	int BattlesWon,
	int BattlesLost,
	int CreditsEarned);
