namespace GrimSpace.Pilots;

public static class PilotSkillPercents
{
	public static class ShieldsUp
	{
		public const int TemporaryShieldPercentOfFaceMax = 25;
	}

	public static class DodgeThis
	{
		public const int AdditionalDamagePercent = 50;

		public static int AdjacentHitDamagePercent => 100 + AdditionalDamagePercent;
	}

	public static class IonizedPayload
	{
		public const int ShieldDamageBonusPercent = 100;
	}

	public static class DefensiveManeuvers
	{
		public const int DeflectChancePercent = 25;

		/// <summary>Fraction of each hit's damage retained after a successful deflect (50 = take half).</summary>
		public const int DeflectDamageTakenPercent = 50;
	}

	public static class InTheFray
	{
		/// <summary>Fraction of allied damage retained (50 = take half).</summary>
		public const int AlliedDamageTakenPercent = 50;
	}

	public static class DronesYearnForMines
	{
		public const int ScrapRewardBonusPercent = 15;
	}

	public static class IKnowABot
	{
		public const int ShipUpgradeDiscountPercent = 10;
	}
}
