namespace GrimSpace.Run.Persistence;

public sealed record RunStatisticsDto(
	int BattlesWon = 0,
	int BattlesLost = 0,
	int CreditsEarned = 0);
