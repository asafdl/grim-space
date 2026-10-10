using System.Text.Json;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.Run.Persistence;

public sealed record StarMapContractDto(
	string Id,
	string ObjectiveType,
	JsonElement Objective,
	EDangerLevel Danger,
	EFaction IssuerFaction,
	ContractTerms Terms,
	ContractNarrative Narrative,
	bool IsStoryObjective,
	string? StateType,
	JsonElement State,
	int? ExpiresAtTick);

internal sealed record MaintainContractBoardDto(
	string ActorId,
	int Tick,
	IReadOnlyList<StarMapContractDto> Additions);
