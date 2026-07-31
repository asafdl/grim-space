namespace GrimSpace.Battle.Weapons;

public static class CombatConfig
{
	public const int DefaultGridSize = 64;

	public const int FlaksPerTurn = 1;
	public const int RailgunsPerTurn = 1;

	public const int FlakRange = 2;
	public const int FlakDamage = 1;
	public const int FlakMomentumLoss = 1;
	public const int FlakResolveDelay = 1;
	public const int FlakApPenaltyThreshold = 2;

	public const int RailgunDamage = 3;
	public const int RailgunLineLength = 8;
	public const int RailgunPyramidRange = 2;
	public const int RailgunMomentumLoss = 0;
	public const int RailgunResolveDelay = 1;

	public const int RollApCost = 1;
	public const int HeadingTurn90ApCost = 1;
	public const int HeadingTurn180ApCost = 2;
}
