namespace GrimSpace.Battle.Weapons;

public static class CombatConfig
{
	public const int DefaultGridSize = 64;

	public const int MissilesPerTurn = 2;
	public const int MissileRadius = 1;

	public static readonly MissileMountConfig DorsalMissile = new()
	{
		Range = 10,
		MinForward = 1,
		MaxAbsRight = 4,
		MinUp = -1,
		MaxUp = 2,
	};

	public const int MissileDamage = 1;
	public const int MissileMomentumLoss = 1;

	public const int RailgunDamage = 999;
	public const int RailgunMaxRange = 30;
	public const int RailgunRequiredTargetMomentum = 0;

	public const int RollApCost = 1;
	public const int HeadingTurn90ApCost = 1;
	public const int HeadingTurn180ApCost = 2;
}
