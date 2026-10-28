using GrimSpace.Units.Enums;

namespace GrimSpace.Pilots;

/// <summary>Rolled pilot identity for a hire slot. Built by <c>PilotRecruitment</c> rolls, materialized via <see cref="Run.Pilot.FromRecruitProfile"/>.</summary>
public sealed class PilotRecruitProfile
{
	private static readonly EPilotSkillId[] EmptySkills = [];

	public string Name { get; }
	public string PortraitId { get; }
	public EPilotBackgroundId BackgroundId { get; }
	public EOperatorClassId ClassId { get; }
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
		int baseSalaryCredits = 10,
		EOperatorClassId classId = EOperatorClassId.SpaceRanger)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(portraitId);
		ArgumentOutOfRangeException.ThrowIfNegative(experiencePoints);
		ArgumentOutOfRangeException.ThrowIfLessThan(claimedLevel, 1);
		ArgumentOutOfRangeException.ThrowIfNegative(baseSalaryCredits);
		if (!Enum.IsDefined(classId) || classId == EOperatorClassId.Unclassified)
			throw new ArgumentOutOfRangeException(
				nameof(classId),
				classId,
				"A pilot must have a playable operator class.");
		learnedSkills ??= EmptySkills;

		ValidateIdentity(backgroundId, positiveQuirkId, negativeQuirkId);

		Name = name;
		PortraitId = portraitId;
		BackgroundId = backgroundId;
		ClassId = classId;
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
