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
	internal static readonly string[] PortraitPool =
	[
		"pilot_01",
		"pilot_02",
		"pilot_03",
		"pilot_04",
		"pilot_05",
		"pilot_06",
		"pilot_07",
		"pilot_08",
		"pilot_09",
		"pilot_10",
	];

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
	public EPilotQuirkId? PositiveQuirkId { get; }
	public EPilotQuirkId? NegativeQuirkId { get; }
	public string? AssignedShipId { get; }
	public bool IsActive { get; }
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
		EPilotQuirkId? positiveQuirkId,
		EPilotQuirkId? negativeQuirkId,
		string? assignedShipId,
		bool isActive,
		int experiencePoints,
		int claimedLevel,
		IReadOnlyList<EPilotSkillId> learnedSkills,
		int baseSalaryCredits,
		int missedSalaryPeriods,
		int missedHazardPeriods)
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
		ValidateIdentity(backgroundId, positiveQuirkId, negativeQuirkId);
		ValidateClassId(classId);

		Id = id;
		Name = name;
		PortraitId = portraitId;
		BackgroundId = backgroundId;
		ClassId = classId;
		PositiveQuirkId = positiveQuirkId;
		NegativeQuirkId = negativeQuirkId;
		AssignedShipId = assignedShipId;
		IsActive = isActive;
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
		return PortraitPool[rng.Next(PortraitPool.Length)];
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
			PortraitPool[rng.Next(PortraitPool.Length)],
			PilotBackgroundCatalog.Roll(rng).Id,
			classId,
			PilotQuirkCatalog.Roll(EPilotQuirkPolarity.Positive, rng),
			PilotQuirkCatalog.Roll(EPilotQuirkPolarity.Negative, rng),
			assignedShipId,
			false,
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
			profile.PositiveQuirkId,
			profile.NegativeQuirkId,
			assignedShipId,
			false,
			profile.ExperiencePoints,
			profile.ClaimedLevel,
			profile.LearnedSkills,
			profile.BaseSalaryCredits,
			0,
			0);
	}

	public static Pilot CreateStarterForShip(string assignedShipId, Random? rng = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(assignedShipId);
		rng ??= Random.Shared;
		return new Pilot(
			TypedIdGenerator.NextId("pilot"),
			DisplayNames.Generate(),
			PortraitPool[rng.Next(PortraitPool.Length)],
			EPilotBackgroundId.ScrapDrone,
			EOperatorClassId.SpaceRanger,
			EPilotQuirkId.Charismatic,
			EPilotQuirkId.NeuroDrugAddict,
			assignedShipId,
			false,
			0,
			PilotProgression.LevelFor(0),
			EmptySkills,
			StrategicEconomyConfig.DefaultBaseSalaryCredits,
			0,
			0);
	}

	internal Pilot WithExperience(int experiencePoints) =>
		Copy(experiencePoints: experiencePoints);

	internal Pilot WithAssignment(string? assignedShipId, bool isActive) =>
		new(
			Id,
			Name,
			PortraitId,
			BackgroundId,
			ClassId,
			PositiveQuirkId,
			NegativeQuirkId,
			assignedShipId,
			isActive,
			ExperiencePoints,
			ClaimedLevel,
			LearnedSkills,
			BaseSalaryCredits,
			MissedSalaryPeriods,
			MissedHazardPeriods);

	internal Pilot WithActive(bool isActive) =>
		Copy(isActive: isActive);

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
		var claimedLevel = dto.ClaimedLevel > 0
			? dto.ClaimedLevel
			: PilotProgression.LevelFor(dto.ExperiencePoints);
		var baseSalary = dto.BaseSalaryCredits > 0
			? dto.BaseSalaryCredits
			: StrategicEconomyConfig.DefaultBaseSalaryCredits;
		return new(
			dto.Id,
			dto.Name,
			dto.PortraitId,
			dto.BackgroundId,
			dto.ClassId,
			dto.PositiveQuirkId,
			dto.NegativeQuirkId,
			dto.AssignedShipId,
			dto.IsActive,
			dto.ExperiencePoints,
			claimedLevel,
			dto.LearnedSkills ?? EmptySkills,
			baseSalary,
			dto.MissedSalaryPeriods,
			dto.MissedHazardPeriods);
	}

	internal PilotPersistenceDto ToPersistence(int activeOrder = -1) =>
		new(
			Id,
			Name,
			PortraitId,
			BackgroundId,
			PositiveQuirkId,
			NegativeQuirkId,
			AssignedShipId,
			ExperiencePoints,
			ClaimedLevel,
			LearnedSkills,
			IsActive,
			IsActive ? activeOrder : -1,
			BaseSalaryCredits,
			MissedSalaryPeriods,
			MissedHazardPeriods,
			ClassId);

	private Pilot Copy(
		bool? isActive = null,
		int? experiencePoints = null,
		int? claimedLevel = null,
		IReadOnlyList<EPilotSkillId>? learnedSkills = null,
		int? missedSalaryPeriods = null,
		int? missedHazardPeriods = null) =>
		new(
			Id,
			Name,
			PortraitId,
			BackgroundId,
			ClassId,
			PositiveQuirkId,
			NegativeQuirkId,
			AssignedShipId,
			isActive ?? IsActive,
			experiencePoints ?? ExperiencePoints,
			claimedLevel ?? ClaimedLevel,
			learnedSkills ?? LearnedSkills,
			BaseSalaryCredits,
			missedSalaryPeriods ?? MissedSalaryPeriods,
			missedHazardPeriods ?? MissedHazardPeriods);

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

	private static void ValidateClassId(EOperatorClassId classId)
	{
		if (!Enum.IsDefined(classId) || classId == EOperatorClassId.Unclassified)
			throw new InvalidDataException(
				$"Pilot class '{classId}' is not a valid pilot qualification.");
	}
}
