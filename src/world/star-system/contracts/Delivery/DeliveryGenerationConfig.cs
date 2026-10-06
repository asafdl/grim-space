namespace GrimSpace.World.StarSystem.Contracts;

public sealed record DeliveryGenerationConfig
{
	public const double DefaultSpaceMeetingChance = 0.2;

	public int FacilityLegCount { get; }
	public double SpaceMeetingChance { get; }

	public DeliveryGenerationConfig(
		int facilityLegCount = 1,
		double spaceMeetingChance = DefaultSpaceMeetingChance)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(facilityLegCount, 1);
		if (spaceMeetingChance is < 0 or > 1)
			throw new ArgumentOutOfRangeException(nameof(spaceMeetingChance));

		FacilityLegCount = facilityLegCount;
		SpaceMeetingChance = spaceMeetingChance;
	}
}
