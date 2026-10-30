namespace GrimSpace.Pilots;

public static class PilotSkillPercents
{
	public static class ShieldsUp
	{
		public const int TemporaryShieldPercentOfFaceMax = 25;
	}

	public static class OverchargeEngines
	{
		/// <summary>Minimum AP required on the ship to activate (does not spend AP).</summary>
		public const int MinimumApToUse = 2;

		public const int RefreshedAp = 2;

		public const int NextTurnApPenalty = 2;

		public const int CooldownTurns = 2;
	}

	public static class DodgeThis
	{
		public const int AdditionalDamagePercent = 50;

		/// <summary>Manhattan cell distance at which the bonus applies (1 cell = 1 km).</summary>
		public const int BonusRangeCells = 2;

		public static int BonusHitDamagePercent => 100 + AdditionalDamagePercent;
	}

	public static class IonizedPayload
	{
		public const int ShieldDamageBonusPercent = 100;
	}

	public static class Brace
	{
		public const int ShieldConsolidationPercent = 66;
	}

	public static class DefensiveManeuvers
	{
		public const int DeflectChancePercent = 25;

		/// <summary>Fraction of each hit's damage retained after a successful deflect (50 = take half).</summary>
		public const int DeflectDamageTakenPercent = 50;
	}

	public static class InTheFray
	{
		/// <summary>Manhattan cell distance required from an enemy (1 cell = 1 km).</summary>
		public const int MinimumDistanceCells = 4;

		public const int MinimumEnemyCount = 2;

		/// <summary>Fraction of incoming hit damage retained after reduction (67 = take 33% less).</summary>
		public const int DamageTakenPercent = 67;
	}

	public static class DronesYearnForMines
	{
		public const int ScrapRewardBonusPercent = 15;
	}

	public static class ReinforcedHull
	{
		/// <summary>Fraction of hull damage retained after reduction (67 = take 33% less).</summary>
		public const int HullDamageTakenPercent = 67;
	}

	public static class BetterRam
	{
		public const int ExperienceGainBonusPercent = 20;
	}

	public static class HelpABotterOut
	{
		public const int HealingRangeBonusCells = 1;
	}

	public static class IKnowABot
	{
		public const int ShipUpgradeDiscountPercent = 10;
	}
}
