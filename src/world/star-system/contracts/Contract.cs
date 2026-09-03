using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.World.StarSystem.Contracts;

public sealed record Contract(
	string Id,
	EContractTask Task,
	EFaction IssuerFaction,
	string? IssuerPoiId,
	AreaPick Location,
	ContractTerms Terms,
	ContractNarrative Narrative);
