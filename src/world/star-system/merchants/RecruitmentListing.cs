using GrimSpace.Math;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public sealed class RecruitmentListing
{
	public string Id { get; }
	public EType Chassis { get; }
	public EShipGearTier GearTier { get; }
	public ResourceBundle Cost { get; private set; }
	public int RollSeed { get; }

	internal RecruitmentListing(
		string id,
		EType chassis,
		EShipGearTier gearTier,
		ResourceBundle cost,
		int? rollSeed = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		Id = id;
		Chassis = chassis;
		GearTier = gearTier;
		Cost = cost;
		RollSeed = rollSeed ?? SeedFor(id);
	}

	public static int SeedFor(string listingId)
	{
		ArgumentException.ThrowIfNullOrEmpty(listingId);
		return unchecked((int)StableSeedMixer.From(0).Add("ship-listing").Add(listingId).Value);
	}

	public RecruitmentOffer ToOffer() => new(Chassis, GearTier, Cost, RollSeed);

	internal RecruitmentListing ForkClone() =>
		new(Id, Chassis, GearTier, CloneBundle(Cost), RollSeed);

	public void SetCost(ResourceBundle cost) => Cost = cost;

	private static ResourceBundle CloneBundle(ResourceBundle bundle)
	{
		var entries = new Dictionary<ResourceId, int>();
		foreach (var (id, amount) in bundle)
			entries[id] = amount;
		return ResourceBundle.Create(entries);
	}
}
