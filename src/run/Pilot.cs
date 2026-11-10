using System.Globalization;
using GrimSpace.Core.Ids;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.Units.Enums;
using UniqueNameGenerator;

namespace GrimSpace.Run;

public sealed class Pilot
{
	private static readonly UniqueName DisplayNames = new UniqueName(
		Adjectives.WordList,
		[Animals.WordList, Colors.WordList])
		.Separator(" ")
		.Format(Style.TitleCase);

	private static readonly EPilotSkillId[] EmptySkills = [];

	public string Id { get; }
	public string Name { get; }
	public string PortraitId { get; }
	public EPilotBackgroundId BackgroundId { get; }
	public EOperatorClassId ClassId { get; }
	/// <summary>Persisted baseline; use EffectivePersonality for disposition queries.</summary>
	public PilotPersonality Personality { get; }
	public PilotPersonality EffectivePersonality =>
		Personality.ApplyModifiers(
			PositiveQuirkIds.Concat(NegativeQuirkIds)
				.Select(id => PilotQuirkCatalog.Get(id).PersonalityModifier)
				.Prepend(PilotBackgroundCatalog.Get(BackgroundId).PersonalityModifier).ToArray());
	public IReadOnlyList<EPilotQuirkId> PositiveQuirkIds { get; }
	public IReadOnlyList<EPilotQuirkId> NegativeQuirkIds { get; }
	public string? AssignedShipId { get; }
	public int ExperiencePoints { get; }
	public int ClaimedLevel { get; }
	public IReadOnlyList<EPilotSkillId> LearnedSkills { get; }
	public int BaseSalaryCredits { get; }
	public int MissedSalaryPeriods { get; }
	public int MissedHazardPeriods { get; }

	private Pilot(
		string id,
		string name,
		string portraitId,
		EPilotBackgroundId backgroundId,
		EOperatorClassId classId,
		IReadOnlyList<EPilotQuirkId> positiveQuirkIds,
		IReadOnlyList<EPilotQuirkId> negativeQuirkIds,
		string? assignedShipId,
		int experiencePoints,
		int claimedLevel,
		IReadOnlyList<EPilotSkillId> learnedSkills,
		int baseSalaryCredits,
		int missedSalaryPeriods,
		int missedHazardPeriods,
		PilotPersonality personality = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(portraitId);
		ArgumentOutOfRangeException.ThrowIfNegative(experiencePoints);
		ArgumentOutOfRangeException.ThrowIfLessThan(claimedLevel, 1);
		ArgumentNullException.ThrowIfNull(learnedSkills);
		ArgumentOutOfRangeException.ThrowIfNegative(baseSalaryCredits);
		ArgumentOutOfRangeException.ThrowIfNegative(missedSalaryPeriods);
		ArgumentOutOfRangeException.ThrowIfNegative(missedHazardPeriods);
		PilotBackgroundCatalog.Get(backgroundId);
		ValidateClassId(classId);

		Id = id;
		Name = name;
		PortraitId = portraitId;
		BackgroundId = backgroundId;
		ClassId = classId;
		Personality = personality;
		PositiveQuirkIds = PilotQuirkCatalog.ValidateQuirks(positiveQuirkIds, EPilotQuirkPolarity.Positive);
		NegativeQuirkIds = PilotQuirkCatalog.ValidateQuirks(negativeQuirkIds, EPilotQuirkPolarity.Negative);
		AssignedShipId = assignedShipId;
		ExperiencePoints = experiencePoints;
		ClaimedLevel = claimedLevel;
		LearnedSkills = learnedSkills;
		BaseSalaryCredits = baseSalaryCredits;
		MissedSalaryPeriods = missedSalaryPeriods;
		MissedHazardPeriods = missedHazardPeriods;
	}

	internal static string RollRecruitDisplayName(Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		var adjective = Adjectives.WordList.GetWord(rng);
		var noun = rng.Next(2) == 0
			? Animals.WordList.GetWord(rng)
			: Colors.WordList.GetWord(rng);
		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase($"{adjective} {noun}");
	}

	internal static string RollRecruitPortraitId(Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		return PilotPortraitId.Roll(rng);
	}

	public static Pilot CreateForShip(
		string assignedShipId,
		EOperatorClassId classId = EOperatorClassId.SpaceRanger,
		Random? rng = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(assignedShipId);
		rng ??= Random.Shared;
		ValidateClassId(classId);
		return new Pilot(
			TypedIdGenerator.NextId("pilot"),
			DisplayNames.Generate(),
			RollRecruitPortraitId(rng),
			PilotBackgroundCatalog.Roll(rng).Id,
			classId,
			[PilotQuirkCatalog.Roll(EPilotQuirkPolarity.Positive, rng)],
			[PilotQuirkCatalog.Roll(EPilotQuirkPolarity.Negative, rng)],
			assignedShipId,
			0,
			PilotProgression.LevelFor(0),
			EmptySkills,
			StrategicEconomyConfig.RollBaseSalaryCredits(rng),
			0,
			0);
	}

	public static Pilot FromRecruitProfile(string? assignedShipId, PilotRecruitProfile profile)
	{
		if (assignedShipId is not null)
			ArgumentException.ThrowIfNullOrEmpty(assignedShipId);
		ArgumentNullException.ThrowIfNull(profile);

		return new Pilot(
			TypedIdGenerator.NextId("pilot"),
			profile.Name,
			profile.PortraitId,
			profile.BackgroundId,
			profile.ClassId,
			profile.PositiveQuirkIds,
			profile.NegativeQuirkIds,
			assignedShipId,
			profile.ExperiencePoints,
			profile.ClaimedLevel,
			profile.LearnedSkills,
			profile.BaseSalaryCredits,
			0,
			0,
			profile.Personality);
	}

	public static Pilot CreateStarterForShip(
		string assignedShipId,
		Random? rng = null,
		EOperatorClassId classId = EOperatorClassId.SpaceRanger)
	{
		ArgumentException.ThrowIfNullOrEmpty(assignedShipId);
		rng ??= Random.Shared;
		ValidateClassId(classId);
		return new Pilot(
			TypedIdGenerator.NextId("pilot"),
			DisplayNames.Generate(),
			RollRecruitPortraitId(rng),
			PilotBackgroundCatalog.Roll(rng).Id,
			classId,
			[EPilotQuirkId.Charismatic],
			[EPilotQuirkId.NeuroDrugAddict],
			assignedShipId,
			0,
			PilotProgression.LevelFor(0),
			EmptySkills,
			StrategicEconomyConfig.DefaultBaseSalaryCredits,
			0,
			0);
	}

	internal Pilot WithExperience(int experiencePoints) =>
		Copy(experiencePoints: experiencePoints);

	internal Pilot WithAssignment(string? assignedShipId) =>
		new(
			Id,
			Name,
			PortraitId,
			BackgroundId,
			ClassId,
			PositiveQuirkIds,
			NegativeQuirkIds,
			assignedShipId,
			ExperiencePoints,
			ClaimedLevel,
			LearnedSkills,
			BaseSalaryCredits,
			MissedSalaryPeriods,
			MissedHazardPeriods,
			Personality);

	internal Pilot WithPersonality(PilotPersonality personality) =>
		Copy(personality: personality);

	internal Pilot WithSalaryPayroll(int missedSalaryPeriods) =>
		Copy(missedSalaryPeriods: missedSalaryPeriods);

	internal Pilot WithHazardMissedPeriods(int missedHazardPeriods) =>
		Copy(missedHazardPeriods: missedHazardPeriods);

	public Pilot LearnPilotSkill(EPilotSkillId skillId)
	{
		if (!PilotSkillAvailability.AvailableToLearn(this).Contains(skillId))
			throw new InvalidOperationException(
				$"Pilot '{Id}' cannot learn skill '{skillId}'.");

		return AppendLearnedSkill(skillId);
	}

	private Pilot AppendLearnedSkill(EPilotSkillId skillId)
	{
		var next = new EPilotSkillId[LearnedSkills.Count + 1];
		for (var index = 0; index < LearnedSkills.Count; index++)
			next[index] = LearnedSkills[index];
		next[^1] = skillId;
		return Copy(claimedLevel: ClaimedLevel + 1, learnedSkills: next);
	}

	internal Pilot Clone() => Copy();

	internal static Pilot FromPersistence(PilotPersistenceDto dto)
	{
		return new(
			dto.Id,
			dto.Name,
			dto.PortraitId,
			dto.BackgroundId,
			dto.ClassId,
			dto.PositiveQuirkIds,
			dto.NegativeQuirkIds,
			dto.AssignedShipId,
			dto.ExperiencePoints,
			dto.ClaimedLevel,
			dto.LearnedSkills,
			dto.BaseSalaryCredits,
			dto.MissedSalaryPeriods,
			dto.MissedHazardPeriods,
			dto.Personality);
	}

	internal PilotPersistenceDto ToPersistence(int deploymentOrder = -1) =>
		new(
			Id,
			Name,
			PortraitId,
			BackgroundId,
			PositiveQuirkIds,
			NegativeQuirkIds,
			AssignedShipId,
			ExperiencePoints,
			ClaimedLevel,
			LearnedSkills,
			deploymentOrder,
			BaseSalaryCredits,
			MissedSalaryPeriods,
			MissedHazardPeriods,
			ClassId,
			Personality);

	private Pilot Copy(
		int? experiencePoints = null,
		int? claimedLevel = null,
		IReadOnlyList<EPilotSkillId>? learnedSkills = null,
		int? missedSalaryPeriods = null,
		int? missedHazardPeriods = null,
		PilotPersonality? personality = null) =>
		new(
			Id,
			Name,
			PortraitId,
			BackgroundId,
			ClassId,
			PositiveQuirkIds,
			NegativeQuirkIds,
			AssignedShipId,
			experiencePoints ?? ExperiencePoints,
			claimedLevel ?? ClaimedLevel,
			learnedSkills ?? LearnedSkills,
			BaseSalaryCredits,
			missedSalaryPeriods ?? MissedSalaryPeriods,
			missedHazardPeriods ?? MissedHazardPeriods,
			personality ?? Personality);

	private static void ValidateClassId(EOperatorClassId classId)
	{
		if (!Enum.IsDefined(classId) || classId == EOperatorClassId.Unclassified)
			throw new InvalidDataException(
				$"Pilot class '{classId}' is not a valid pilot qualification.");
	}
}
