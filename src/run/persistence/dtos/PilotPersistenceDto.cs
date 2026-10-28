using GrimSpace.Pilots;

namespace GrimSpace.Run.Persistence;

public sealed record PilotPersistenceDto(
	string Id,
	string Name,
	string PortraitId,
	EPilotBackgroundId BackgroundId,
	EPilotQuirkId? PositiveQuirkId,
	EPilotQuirkId? NegativeQuirkId,
	string? AssignedShipId,
	int ExperiencePoints = 0,
	int ClaimedLevel = 0,
	IReadOnlyList<EPilotSkillId>? LearnedSkills = null,
	bool IsActive = false,
	int ActiveOrder = -1,
	int BaseSalaryCredits = 0,
	int MissedSalaryPeriods = 0,
	int MissedHazardPeriods = 0);
