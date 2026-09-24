using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed record ContractDifficultyProfile(
	HuntEncounterArgs HuntEncounter,
	int HuntRewardCredits,
	int DeliveryRewardCredits,
	int WreckageRewardCredits = 60,
	int WreckageMinimumPoiClearance = 16,
	float WreckageSalvageWeight = 0.6f,
	int WreckageSalvageScrapAlloy = 3,
	float WreckageBorderReferenceWeight = 0.75f);
