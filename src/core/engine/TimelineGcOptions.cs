namespace GrimSpace.Core.Engine;

internal sealed record TimelineGcOptions(TimeSpan Interval, int RetentionTicks)
{
	public static TimelineGcOptions Default { get; } = new(TimeSpan.FromSeconds(30), 1000);

	public static TimelineGcOptions Disabled { get; } = new(TimeSpan.Zero, 0);
}
