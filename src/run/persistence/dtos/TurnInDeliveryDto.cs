namespace GrimSpace.Run.Persistence;

internal sealed record TurnInDeliveryDto(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	string ContractId);
