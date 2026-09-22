namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public sealed record DeliveryObjective(
	string TurnInPoiId,
	string TurnInFacilityId,
	string TurnInOperatorName) : IContractObjective;
