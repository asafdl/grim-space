using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts;

public sealed record Contract(
	string Id,
	IContractObjective Objective,
	EFaction IssuerFaction,
	string? IssuerPoiId,
	ContractTerms Terms,
	ContractNarrative Narrative);
