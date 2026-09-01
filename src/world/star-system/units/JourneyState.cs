using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Units;

public sealed class JourneyState
{
	public long JourneyId { get; set; }
	public Coord Origin { get; set; }
	public Coord Destination { get; set; }
	public int StartTick { get; set; }

	public bool IsActive => JourneyId != 0;

	public (Coord Position, Coord Tangent) SamplePosition(
		TransitPath path,
		double elapsedTicks,
		double speedPerTick) =>
		path.SampleAtElapsed(elapsedTicks, speedPerTick);

	public JourneyState Clone() =>
		new()
		{
			JourneyId = JourneyId,
			Origin = Origin,
			Destination = Destination,
			StartTick = StartTick,
		};

	public void Clear()
	{
		JourneyId = 0;
		Origin = default;
		Destination = default;
		StartTick = 0;
	}
}
