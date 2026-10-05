namespace GrimSpace.World.StarSystem.Contracts;

public sealed record DeliveryGenerationConfig
{
	public int FacilityLegCount { get; }

	public DeliveryGenerationConfig(int facilityLegCount = 1)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(facilityLegCount, 1);
		FacilityLegCount = facilityLegCount;
	}
}
