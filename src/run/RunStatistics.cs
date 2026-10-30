using GrimSpace.Battle.Objectives;
using GrimSpace.Run.Persistence;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run;

public sealed class RunStatistics
{
	public int BattlesWon { get; private set; }
	public int BattlesLost { get; private set; }
	public int CreditsEarned { get; private set; }

	public void RecordBattle(EBattleResult result)
	{
		switch (result)
		{
			case EBattleResult.Win:
				BattlesWon++;
				break;
			case EBattleResult.Lose:
				BattlesLost++;
				break;
		}
	}

	public void RecordTransaction(Transaction transaction)
	{
		ArgumentNullException.ThrowIfNull(transaction);
		if (transaction.Source == TransactionSource.DebugGrant)
			return;
		if (!transaction.Change.TryGet(ResourceId.Credits, out var credits) || credits <= 0)
			return;

		CreditsEarned += credits;
	}

	public RunStatisticsDto CaptureSnapshot() =>
		new(BattlesWon, BattlesLost, CreditsEarned);

	public void RestoreSnapshot(RunStatisticsDto? dto)
	{
		BattlesWon = dto?.BattlesWon ?? 0;
		BattlesLost = dto?.BattlesLost ?? 0;
		CreditsEarned = dto?.CreditsEarned ?? 0;
	}
}

public sealed record RunStatisticsSummary(
	int ContractsCompleted,
	int BattlesWon,
	int BattlesLost,
	int CreditsEarned,
	int CreditsRemaining);
