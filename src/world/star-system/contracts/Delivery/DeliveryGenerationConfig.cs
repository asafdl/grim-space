using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts;

public sealed record DeliveryGenerationConfig
{
	public const double DefaultSpaceMeetingChance = 0.2;
	public const double DefaultDeadlineSlackMultiplier = 1.6;
	public const int DefaultInterruptionBufferTicks = 15;
	public const int DefaultInterceptionLeadTicks = 5;
	public const int DefaultInterceptionRetryDelayTicks = 5;

	public static DeliveryGenerationConfig Default { get; } = new();

	public int FacilityLegCount { get; }
	public double SpaceMeetingChance { get; }
	public double DeadlineSlackMultiplier { get; }
	public int InterruptionBufferTicks { get; }
	public int InterceptionLeadTicks { get; }
	public int InterceptionRetryDelayTicks { get; }

	public DeliveryGenerationConfig(
		int facilityLegCount = 1,
		double spaceMeetingChance = DefaultSpaceMeetingChance,
		double deadlineSlackMultiplier = DefaultDeadlineSlackMultiplier,
		int interruptionBufferTicks = DefaultInterruptionBufferTicks,
		int interceptionLeadTicks = DefaultInterceptionLeadTicks,
		int interceptionRetryDelayTicks = DefaultInterceptionRetryDelayTicks)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(facilityLegCount, 1);
		if (spaceMeetingChance is < 0 or > 1)
			throw new ArgumentOutOfRangeException(nameof(spaceMeetingChance));
		if (deadlineSlackMultiplier <= 0)
			throw new ArgumentOutOfRangeException(nameof(deadlineSlackMultiplier));
		ArgumentOutOfRangeException.ThrowIfNegative(interruptionBufferTicks);
		ArgumentOutOfRangeException.ThrowIfNegative(interceptionLeadTicks);
		ArgumentOutOfRangeException.ThrowIfLessThan(interceptionRetryDelayTicks, 1);

		FacilityLegCount = facilityLegCount;
		SpaceMeetingChance = spaceMeetingChance;
		DeadlineSlackMultiplier = deadlineSlackMultiplier;
		InterruptionBufferTicks = interruptionBufferTicks;
		InterceptionLeadTicks = interceptionLeadTicks;
		InterceptionRetryDelayTicks = interceptionRetryDelayTicks;
	}

	public double InterceptionChanceFor(EDangerLevel danger) =>
		danger switch
		{
			EDangerLevel.VeryLow => 0.0,
			EDangerLevel.Low => 0.15,
			EDangerLevel.Moderate => 0.35,
			EDangerLevel.High => 0.60,
			EDangerLevel.VeryHigh => 0.85,
			_ => throw new ArgumentOutOfRangeException(nameof(danger), danger, null),
		};
}
