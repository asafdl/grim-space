using System.Text.Json.Serialization;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BlackMarketFacilityDestination), "facility")]
[JsonDerivedType(typeof(BlackMarketSpaceMeetingDestination), "space-meeting")]
public abstract record BlackMarketSaleDestination;

public sealed record BlackMarketFacilityDestination(
	string PoiId,
	string FacilityId) : BlackMarketSaleDestination;

public sealed record BlackMarketSpaceMeetingDestination(
	string MeetingId,
	string ContactName,
	AreaPick SearchArea) : BlackMarketSaleDestination
{
	public Coord Position => SearchArea.SpawnPoints[0];
}

public sealed record BlackMarketSaleObjective : ILeadObjective
{
	public ELeadKind LeadKind => ELeadKind.BlackMarketSale;
	public BlackMarketSaleDestination Destination { get; }
	public EMerchantSubtype MerchantSubtype { get; }
	public int DurationTicks { get; }

	public BlackMarketSaleObjective(
		BlackMarketSaleDestination destination,
		EMerchantSubtype merchantSubtype,
		int durationTicks)
	{
		ArgumentNullException.ThrowIfNull(destination);
		if (!Enum.IsDefined(merchantSubtype))
			throw new ArgumentOutOfRangeException(nameof(merchantSubtype), merchantSubtype, null);
		ArgumentOutOfRangeException.ThrowIfLessThan(durationTicks, 1);

		Destination = destination;
		MerchantSubtype = merchantSubtype;
		DurationTicks = durationTicks;
	}
}
