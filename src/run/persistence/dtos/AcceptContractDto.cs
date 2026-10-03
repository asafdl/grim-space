namespace GrimSpace.Run.Persistence;

internal sealed record AcceptContractDto(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	string ContractId,
	string SpawnIdentity);
