namespace GrimSpace.Run.Persistence;

internal sealed record DeclineContractDto(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	string ContractId);
