namespace GrimSpace.Pilots;

/// <summary>Rolled pilot identity for a hire slot. Built by <c>PilotRecruitment</c> rolls, materialized via <see cref="Run.Pilot.FromRecruitProfile"/>.</summary>
public sealed class PilotRecruitProfile
{
	private static readonly EPilotSkillId[] EmptySkills = [];

	public string Name { get; }
	public string PortraitId { get; }
	public EPilotBackgroundId BackgroundId { get; }
	public EPilotQuirkId? PositiveQuirkId { get; }
	public EPilotQuirkId? NegativeQuirkId { get; }
	public int ExperiencePoints { get; }
	public int ClaimedLevel { get; }
	public IReadOnlyList<EPilotSkillId> LearnedSkills { get; }
	public int BaseSalaryCredits { get; }

	public PilotRecruitProfile(
		string name,
		string portraitId,
		EPilotBackgroundId backgroundId,
		EPilotQuirkId? positiveQuirkId = null,
		EPilotQuirkId? negativeQuirkId = null,
		int experiencePoints = 0,
		int claimedLevel = 1,
		IReadOnlyList<EPilotSkillId>? learnedSkills = null,
		int baseSalaryCredits = 10)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(portraitId);
		ArgumentOutOfRangeException.ThrowIfNegative(experiencePoints);
		ArgumentOutOfRangeException.ThrowIfLessThan(claimedLevel, 1);
		ArgumentOutOfRangeException.ThrowIfNegative(baseSalaryCredits);
		learnedSkills ??= EmptySkills;

		ValidateIdentity(backgroundId, positiveQuirkId, negativeQuirkId);

		Name = name;
		PortraitId = portraitId;
		BackgroundId = backgroundId;
		PositiveQuirkId = positiveQuirkId;
		NegativeQuirkId = negativeQuirkId;
		ExperiencePoints = experiencePoints;
		ClaimedLevel = claimedLevel;
		LearnedSkills = learnedSkills.Count == 0 ? EmptySkills : learnedSkills.ToArray();
		BaseSalaryCredits = baseSalaryCredits;
	}

	private static void ValidateIdentity(
		EPilotBackgroundId backgroundId,
		EPilotQuirkId? positiveQuirkId,
		EPilotQuirkId? negativeQuirkId)
	{
		PilotBackgroundCatalog.Get(backgroundId);

		if (positiveQuirkId is { } positive)
		{
			var positiveQuirk = PilotQuirkCatalog.Get(positive);
			if (positiveQuirk.Polarity != EPilotQuirkPolarity.Positive)
				throw new ArgumentException(
					$"Pilot positive quirk '{positive}' has polarity '{positiveQuirk.Polarity}'.",
					nameof(positiveQuirkId));
		}

		if (negativeQuirkId is { } negative)
		{
			var negativeQuirk = PilotQuirkCatalog.Get(negative);
			if (negativeQuirk.Polarity != EPilotQuirkPolarity.Negative)
				throw new ArgumentException(
					$"Pilot negative quirk '{negative}' has polarity '{negativeQuirk.Polarity}'.",
					nameof(negativeQuirkId));
		}
	}
}
