namespace GrimSpace.Battle.Objectives;

public readonly record struct BattleOutcome(EBattleResult Result)
{
	public bool IsOver => Result != EBattleResult.Ongoing;

	public static BattleOutcome Ongoing { get; } = new(EBattleResult.Ongoing);
	public static BattleOutcome Win { get; } = new(EBattleResult.Win);
	public static BattleOutcome Lose { get; } = new(EBattleResult.Lose);
	public static BattleOutcome Tie { get; } = new(EBattleResult.Tie);
}
