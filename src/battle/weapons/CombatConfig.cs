namespace GrimSpace.Battle.Weapons;

public static class CombatConfig
{
	public const int DefaultGridSize = 64;

	public const int MissilesPerTurn = 2;
	public const int MissileRadius = 1;

	public static readonly MissileMountConfig ForeMissile = new()
	{
		Range = 10,
		MinFore = 1,
		MaxAbsPort = 4,
		MinDorsal = -1,
		MaxDorsal = 2,
	};

	public const int ForeMissileMinRange = 10;
	public const int ForeMissileMaxRange = 15;

	public const int MissileDamage = 1;
	public const int MissileMomentumLoss = 1;
	public const int MissileResolveDelay = 2;

	public const int FlakRange = 3;
	public const int FlakMomentumLoss = 1;
	public const int FlakResolveDelay = 1;
	public const int FlakApPenaltyThreshold = 2;

	public const int RailgunDamage = 999;
	public const int RailgunMaxRange = 30;
	public const int RailgunRequiredTargetMomentum = 0;

	public const int RollApCost = 1;
	public const int HeadingTurn90ApCost = 1;
	public const int HeadingTurn180ApCost = 2;
}
