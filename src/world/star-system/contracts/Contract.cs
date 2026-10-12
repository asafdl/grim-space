using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts;

public enum EContractCategory
{
	Standard,
	Lead,
}

public sealed record Contract(
	string Id,
	IContractObjective Objective,
	EDangerLevel Danger,
	EFaction IssuerFaction,
	ContractTerms Terms,
	ContractNarrative Narrative,
	bool IsStoryObjective = false,
	EContractCategory Category = EContractCategory.Standard)
{
	public bool AllowsDecline => !IsStoryObjective;
}
