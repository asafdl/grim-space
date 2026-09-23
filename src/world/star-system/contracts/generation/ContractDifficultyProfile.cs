using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed record ContractDifficultyProfile(
	HuntEncounterArgs HuntEncounter,
	int HuntRewardCredits,
	int DeliveryRewardCredits);
