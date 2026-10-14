using GrimSpace.Core.Ids;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
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
	public string? AssignedShipId { get; }
	public bool IsActive { get; }
	public int ExperiencePoints { get; }
	public int ClaimedLevel { get; }
	public IReadOnlyList<EPilotSkillId> LearnedSkills { get; }

	private Pilot(
		string id,
		string name,
		string portraitId,
		string? assignedShipId,
		bool isActive,
		int experiencePoints,
		int claimedLevel,
		IReadOnlyList<EPilotSkillId> learnedSkills)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(portraitId);
		ArgumentOutOfRangeException.ThrowIfNegative(experiencePoints);
		ArgumentOutOfRangeException.ThrowIfLessThan(claimedLevel, 1);
		ArgumentNullException.ThrowIfNull(learnedSkills);

		Id = id;
		Name = name;
		PortraitId = portraitId;
		AssignedShipId = assignedShipId;
		IsActive = isActive;
		ExperiencePoints = experiencePoints;
		ClaimedLevel = claimedLevel;
		LearnedSkills = learnedSkills;
	}

	public static Pilot CreateForShip(string assignedShipId, Random? rng = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(assignedShipId);
		rng ??= Random.Shared;
		return new Pilot(
			TypedIdGenerator.NextId("pilot"),
			DisplayNames.Generate(),
			PortraitPool[rng.Next(PortraitPool.Length)],
			assignedShipId,
			false,
			0,
			PilotProgression.LevelFor(0),
			EmptySkills);
	}

	internal Pilot WithExperience(int experiencePoints) =>
		new(
			Id,
			Name,
			PortraitId,
			AssignedShipId,
			IsActive,
			experiencePoints,
			ClaimedLevel,
			LearnedSkills);

	internal Pilot WithAssignment(string? assignedShipId, bool isActive) =>
		new(
			Id,
			Name,
			PortraitId,
			assignedShipId,
			isActive,
			ExperiencePoints,
			ClaimedLevel,
			LearnedSkills);

	internal Pilot WithActive(bool isActive) =>
		new(
			Id,
			Name,
			PortraitId,
			AssignedShipId,
			isActive,
			ExperiencePoints,
			ClaimedLevel,
			LearnedSkills);

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
		return new(
			Id,
			Name,
			PortraitId,
			AssignedShipId,
			IsActive,
			ExperiencePoints,
			ClaimedLevel + 1,
			next);
	}

	internal Pilot Clone() =>
		new(
			Id,
			Name,
			PortraitId,
			AssignedShipId,
			IsActive,
			ExperiencePoints,
			ClaimedLevel,
			LearnedSkills);

	internal static Pilot FromPersistence(PilotPersistenceDto dto)
	{
		var claimedLevel = dto.ClaimedLevel > 0
			? dto.ClaimedLevel
			: PilotProgression.LevelFor(dto.ExperiencePoints);
		return new(
			dto.Id,
			dto.Name,
			dto.PortraitId,
			dto.AssignedShipId,
			dto.IsActive,
			dto.ExperiencePoints,
			claimedLevel,
			dto.LearnedSkills ?? EmptySkills);
	}

	internal PilotPersistenceDto ToPersistence(int activeOrder = -1) =>
		new(
			Id,
			Name,
			PortraitId,
			AssignedShipId,
			ExperiencePoints,
			ClaimedLevel,
			LearnedSkills,
			IsActive,
			IsActive ? activeOrder : -1);
}
