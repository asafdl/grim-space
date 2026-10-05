using System.Text.Json.Serialization;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FacilityDeliveryLeg), "facility")]
[JsonDerivedType(typeof(SpaceMeetingDeliveryLeg), "space-meeting")]
public abstract record DeliveryLeg;

public sealed record FacilityDeliveryLeg(
	string PoiId,
	string FacilityId,
	string OperatorName) : DeliveryLeg;

public sealed record SpaceMeetingDeliveryLeg(
	string MeetingId,
	string ContactName) : DeliveryLeg;

public sealed record DeliveryRoute
{
	public IReadOnlyList<DeliveryLeg> Legs { get; }

	public DeliveryRoute(IReadOnlyList<DeliveryLeg> legs)
	{
		ArgumentNullException.ThrowIfNull(legs);
		if (legs.Count == 0)
			throw new ArgumentException("A delivery route must contain at least one leg.", nameof(legs));
		Legs = legs;
	}
}

public sealed record DeliveryObjective : IContractObjective
{
	public DeliveryRoute Route { get; }

	// Kept as compatibility projections for existing callers and legacy saves.
	public string TurnInPoiId => FinalFacility.PoiId;
	public string TurnInFacilityId => FinalFacility.FacilityId;
	public string TurnInOperatorName => FinalFacility.OperatorName;
	public int RouteLegCount => Route.Legs.Count;

	public DeliveryObjective(
		string turnInPoiId,
		string turnInFacilityId,
		string turnInOperatorName)
		: this(new DeliveryRoute(
			[new FacilityDeliveryLeg(turnInPoiId, turnInFacilityId, turnInOperatorName)]))
	{
	}

	public DeliveryObjective(DeliveryRoute route)
	{
		ArgumentNullException.ThrowIfNull(route);
		if (route.Legs[^1] is not FacilityDeliveryLeg)
			throw new ArgumentException("A delivery route must end at a facility.", nameof(route));
		Route = route;
	}

	private FacilityDeliveryLeg FinalFacility =>
		(FacilityDeliveryLeg)Route.Legs[^1];
}
