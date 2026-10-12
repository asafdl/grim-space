using System.Text.Json.Serialization;

namespace GrimSpace.World.StarSystem.Merchants;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FacilityMerchantEndpoint), "facility")]
[JsonDerivedType(typeof(LeadSaleMerchantEndpoint), "lead-sale")]
public abstract record MerchantEndpoint;

public sealed record FacilityMerchantEndpoint(
	string PoiId,
	string FacilityId,
	string OperatorName) : MerchantEndpoint;

public sealed record LeadSaleMerchantEndpoint(string ContractId) : MerchantEndpoint;
