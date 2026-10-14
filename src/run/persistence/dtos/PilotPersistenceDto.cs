using GrimSpace.Pilots;

namespace GrimSpace.Run.Persistence;

public sealed record PilotPersistenceDto(
	string Id,
	string Name,
	string PortraitId,
	string? AssignedShipId,
	int ExperiencePoints = 0,
	int ClaimedLevel = 0,
	IReadOnlyList<EPilotSkillId>? LearnedSkills = null,
	bool IsActive = false,
	int ActiveOrder = -1);
