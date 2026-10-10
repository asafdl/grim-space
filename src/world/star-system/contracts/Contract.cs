using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts;

public sealed record Contract(
	string Id,
	IContractObjective Objective,
	EDangerLevel Danger,
	EFaction IssuerFaction,
	ContractTerms Terms,
	ContractNarrative Narrative,
	bool IsStoryObjective = false)
{
	public bool AllowsDecline => !IsStoryObjective;
}
