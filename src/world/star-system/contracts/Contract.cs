using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts;

public delegate bool IsMetDelegate(string contractId, StarMap map, string actorId);

public sealed record Contract(
	string Id,
	IContractObjective Objective,
	EDangerLevel Danger,
	EFaction IssuerFaction,
	string? IssuerPoiId,
	ContractTerms Terms,
	ContractNarrative Narrative,
	IsMetDelegate ObjectiveMet,
	bool IsStoryObjective = false)
{
	public bool AllowsDecline => !IsStoryObjective;
}
