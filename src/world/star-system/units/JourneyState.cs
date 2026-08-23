namespace GrimSpace.World.StarSystem.Units;

public sealed class JourneyState
{
	public string? RouteId { get; set; }
	public bool TowardDockB { get; set; }
	public double LongitudinalProgress { get; set; }
	public double LateralOffset { get; set; }

	public JourneyState Clone() =>
		new()
		{
			RouteId = RouteId,
			TowardDockB = TowardDockB,
			LongitudinalProgress = LongitudinalProgress,
			LateralOffset = LateralOffset,
		};

	public void Clear()
	{
		RouteId = null;
		TowardDockB = false;
		LongitudinalProgress = 0;
		LateralOffset = 0;
	}
}
