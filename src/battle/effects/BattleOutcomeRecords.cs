using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

internal static class BattleOutcomeRecords
{
	public static IReadOnlyList<IRecord> ForResult(BattleWorld world, EBattleResult result) =>
		result == EBattleResult.Ongoing
			? []
			: [new Record<BattleOutcome>(CommitBattleOutcomeRules.CaptureOutcome(world, result))];
}
