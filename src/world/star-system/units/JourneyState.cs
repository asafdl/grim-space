using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Units;

public sealed class JourneyState
{
	public string? DestinationDockId { get; set; }
	public TransitPath? Path { get; set; }
	public int LegIndex { get; set; }
	public double LegProgress { get; set; }

	public (Coord Position, Coord Tangent) SamplePosition(float tickFraction, double speedPerTick)
	{
		var path = Path ?? throw new InvalidOperationException("Journey has no transit path.");
		if (LegIndex >= path.Legs.Length)
			throw new InvalidOperationException("Journey leg index is out of range.");

		var leg = path.Legs[LegIndex];
		var progress = LegProgress + tickFraction * speedPerTick * leg.SpeedMultiplier;
		progress = System.Math.Min(progress, leg.Length);
		return PolylineSampler.Sample(leg.Points, progress);
	}

	public JourneyState Clone() =>
		new()
		{
			DestinationDockId = DestinationDockId,
			Path = Path,
			LegIndex = LegIndex,
			LegProgress = LegProgress,
		};

	public void Clear()
	{
		DestinationDockId = null;
		Path = null;
		LegIndex = 0;
		LegProgress = 0;
	}
}
