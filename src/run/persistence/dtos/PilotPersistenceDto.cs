using GrimSpace.Pilots;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run.Persistence;

public sealed record PilotPersistenceDto(
	string Id,
	string Name,
	string PortraitId,
	EPilotBackgroundId BackgroundId,
	IReadOnlyList<EPilotQuirkId> PositiveQuirkIds,
	IReadOnlyList<EPilotQuirkId> NegativeQuirkIds,
	string? AssignedShipId,
	int ExperiencePoints,
	int ClaimedLevel,
	IReadOnlyList<EPilotSkillId> LearnedSkills,
	bool IsActive,
	int ActiveOrder,
	int BaseSalaryCredits,
	int MissedSalaryPeriods,
	int MissedHazardPeriods,
	EOperatorClassId ClassId,
	PilotPersonality Personality);
