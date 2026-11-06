namespace GrimSpace.Pilots;

public static class PilotSkillPercents
{
	public static class ShieldsUp
	{
		public const int TemporaryShieldPercentOfFaceMax = 25;
	}

	public static class OverchargeEngines
	{
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
		/// <summary>Fraction of each hit's damage retained while stationary (90 = take 10% less).</summary>
		public const int DamageTakenPercent = 90;
	}

	public static class ShieldDynamo
	{
		public const int ShieldsPerFacePerAp = 5;
	}

	public static class MarkWeakspot
	{
		public const int RangeCells = 6;
		public const int ShieldDamageBonusPercent = 20;
		public const int CooldownTurns = 3;
		public const int DurationEndPasses = 2;
	}

	public static class ThirstForScrap
	{
		public const int DamageBonusPercentPerKill = 10;
	}

	public static class YouLittleRat
	{
		public const int MinimumDistanceCells = 6;
		public const int DamageTakenPercent = 80;
	}

	public static class NothingButMetal
	{
		public const int DamageBonusPercent = 40;
	}

	public static class NotFullyFormed
	{
		public const int SpawnedUnitDamageBonusPercent = 30;
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

	public static class HeatVents
	{
		public const int HeatReleased = 40;

		public const int ApCost = 1;
	}

	public static class ReactiveShields
	{
		/// <summary>Fraction of hit damage retained per stack (80 = each stack reduces by 20%, multiplicative).</summary>
		public const int DamageTakenPercentPerStack = 80;
	}

	public static class HelpABotterOut
	{
		public const int HealingRangeBonusCells = 1;
	}

	public static class ScrapDrone
	{
		public const int SwarmDamageBonusPercent = 15;
	}

	public static class PodRacer
	{
		public const int HeatGainReductionPercent = 20;
	}

	public static class AsteroidSurfer
	{
		public const int OutgoingDamageBonusPercent = 10;

		/// <summary>Additive hull damage taken bonus (115 = take 15% more hull damage).</summary>
		public const int HullDamageTakenPercent = 100 + 15;
	}

	public static class RetiredMachineManager
	{
		public const int SpawnedAllyDamageBonusPercent = 20;
	}

	public static class IKnowABot
	{
		public const int ShipUpgradeDiscountPercent = 10;
	}
}
