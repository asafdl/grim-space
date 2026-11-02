using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class LeadGenerationConfig
{
	public const double DefaultGenerationChance = 0.22;
	public const double DefaultSpaceMeetingChance = 0.2;
	public const int DefaultMaxOccupancy = 1;
	public const int DefaultSaleDurationTicks = 300;
	public const int DefaultMinGapTicks = 50;

	public double GenerationChance { get; init; } = DefaultGenerationChance;

	public int MaxOccupancy { get; init; } = DefaultMaxOccupancy;

	public int SaleDurationTicks { get; init; } = DefaultSaleDurationTicks;

	/// <summary>Ticks after the previous accepted sale's deadline before another lead may roll.</summary>
	public int MinGapTicks { get; init; } = DefaultMinGapTicks;

	public double SpaceMeetingChance { get; init; } = DefaultSpaceMeetingChance;

	public IReadOnlyDictionary<ELeadKind, float> KindWeights { get; init; } =
		new Dictionary<ELeadKind, float>
		{
			[ELeadKind.BlackMarketSale] = 1f,
		};
}
