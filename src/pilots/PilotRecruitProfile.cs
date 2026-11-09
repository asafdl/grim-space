using GrimSpace.Units.Enums;

namespace GrimSpace.Pilots;

/// <summary>Rolled pilot identity for a hire slot. Built by <c>PilotRecruitment</c> rolls, materialized via <see cref="Run.Pilot.FromRecruitProfile"/>.</summary>
public sealed class PilotRecruitProfile : IEquatable<PilotRecruitProfile>
{
	private static readonly EPilotSkillId[] EmptySkills = [];

	public string Name { get; }
	public string PortraitId { get; }
	public EPilotBackgroundId BackgroundId { get; }
	public EOperatorClassId ClassId { get; }
	public PilotPersonality Personality { get; }
	public IReadOnlyList<EPilotQuirkId> PositiveQuirkIds { get; }
	public IReadOnlyList<EPilotQuirkId> NegativeQuirkIds { get; }
	public int ExperiencePoints { get; }
	public int ClaimedLevel { get; }
	public IReadOnlyList<EPilotSkillId> LearnedSkills { get; }
	public int BaseSalaryCredits { get; }

	public PilotRecruitProfile(
		string name,
		string portraitId,
		EPilotBackgroundId backgroundId,
		IReadOnlyList<EPilotQuirkId>? positiveQuirkIds = null,
		IReadOnlyList<EPilotQuirkId>? negativeQuirkIds = null,
		int experiencePoints = 0,
		int claimedLevel = 1,
		IReadOnlyList<EPilotSkillId>? learnedSkills = null,
		int baseSalaryCredits = 10,
		EOperatorClassId classId = EOperatorClassId.SpaceRanger,
		PilotPersonality personality = default)
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

		PilotBackgroundCatalog.Get(backgroundId);

		Name = name;
		PortraitId = portraitId;
		BackgroundId = backgroundId;
		ClassId = classId;
		Personality = personality;
		PositiveQuirkIds = PilotQuirkCatalog.ValidateQuirks(positiveQuirkIds ?? [], EPilotQuirkPolarity.Positive);
		NegativeQuirkIds = PilotQuirkCatalog.ValidateQuirks(negativeQuirkIds ?? [], EPilotQuirkPolarity.Negative);
		ExperiencePoints = experiencePoints;
		ClaimedLevel = claimedLevel;
		LearnedSkills = learnedSkills.Count == 0 ? EmptySkills : learnedSkills.ToArray();
		BaseSalaryCredits = baseSalaryCredits;
	}

	public bool Equals(PilotRecruitProfile? other) =>
		other is not null
		&& Name == other.Name
		&& PortraitId == other.PortraitId
		&& BackgroundId == other.BackgroundId
		&& ClassId == other.ClassId
		&& Personality.Equals(other.Personality)
		&& PositiveQuirkIds.SequenceEqual(other.PositiveQuirkIds)
		&& NegativeQuirkIds.SequenceEqual(other.NegativeQuirkIds)
		&& ExperiencePoints == other.ExperiencePoints
		&& ClaimedLevel == other.ClaimedLevel
		&& LearnedSkills.SequenceEqual(other.LearnedSkills)
		&& BaseSalaryCredits == other.BaseSalaryCredits;

	public override bool Equals(object? obj) => Equals(obj as PilotRecruitProfile);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(Name);
		hash.Add(PortraitId);
		hash.Add(BackgroundId);
		hash.Add(ClassId);
		hash.Add(Personality);
		foreach (var quirkId in PositiveQuirkIds)
			hash.Add(quirkId);
		foreach (var quirkId in NegativeQuirkIds)
			hash.Add(quirkId);
		hash.Add(ExperiencePoints);
		hash.Add(ClaimedLevel);
		foreach (var skillId in LearnedSkills)
			hash.Add(skillId);
		hash.Add(BaseSalaryCredits);
		return hash.ToHashCode();
	}
}
