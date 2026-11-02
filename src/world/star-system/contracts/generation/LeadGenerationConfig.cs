using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class LeadGenerationConfig
{
	public const double DefaultSpaceMeetingChance = 0.2;
	public const int DefaultMaxOccupancy = 1;
	public const int DefaultSaleDurationTicks = 300;
	public const int DefaultIntervalTicks = 400;

	/// <summary>A lead is offered on every positive multiple of this tick count; zero disables leads.</summary>
	public int IntervalTicks { get; init; } = DefaultIntervalTicks;

	public int MaxOccupancy { get; init; } = DefaultMaxOccupancy;

	public int SaleDurationTicks { get; init; } = DefaultSaleDurationTicks;

	public double SpaceMeetingChance { get; init; } = DefaultSpaceMeetingChance;

	public IReadOnlyDictionary<ELeadKind, float> KindWeights { get; init; } =
		new Dictionary<ELeadKind, float>
		{
			[ELeadKind.BlackMarketSale] = 1f,
		};
}
