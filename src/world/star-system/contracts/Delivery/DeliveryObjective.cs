using System.Text.Json.Serialization;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;

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
	string ContactName,
	AreaPick SearchArea) : DeliveryLeg
{
	public Coord Position => SearchArea.SpawnPoints[0];
}

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
	public string PickupPoiId { get; }
	public DeliveryRoute Route { get; }
	public DeliveryGenerationConfig Config { get; }

	// Kept as compatibility projections for existing callers and legacy saves.
	public string TurnInPoiId => FinalFacility.PoiId;
	public string TurnInFacilityId => FinalFacility.FacilityId;
	public string TurnInOperatorName => FinalFacility.OperatorName;
	public int RouteLegCount => Route.Legs.Count;

	public DeliveryObjective(
		string pickupPoiId,
		string turnInPoiId,
		string turnInFacilityId,
		string turnInOperatorName)
		: this(
			pickupPoiId,
			new DeliveryRoute(
				[new FacilityDeliveryLeg(turnInPoiId, turnInFacilityId, turnInOperatorName)]),
			DeliveryGenerationConfig.Default)
	{
	}

	public DeliveryObjective(
		string pickupPoiId,
		DeliveryRoute route,
		DeliveryGenerationConfig? config = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(pickupPoiId);
		ArgumentNullException.ThrowIfNull(route);
		if (route.Legs[^1] is not FacilityDeliveryLeg)
			throw new ArgumentException("A delivery route must end at a facility.", nameof(route));
		PickupPoiId = pickupPoiId;
		Route = route;
		Config = config ?? DeliveryGenerationConfig.Default;
	}

	private FacilityDeliveryLeg FinalFacility =>
		(FacilityDeliveryLeg)Route.Legs[^1];
}
