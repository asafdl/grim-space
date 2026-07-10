using GrimSpace.Domain.Grid;
using GrimSpace.Domain.Units;

namespace GrimSpace.Battle.Units;

public sealed class State
{
	public required string Id { get; init; }
	public Coord Position { get; set; }
	public Coord ForwardDirection { get; set; }
	public Coord UpDirection { get; set; }
	public Coord RightDirection { get; set; }
	public int ActionPoints { get; set; }
	public required Stats Stats { get; init; }

	public static State FromSpawn(Instance instance, Coord position)
	{
		var stats = Stats.ForType(instance.Type);
		var fwd = Coord.Forward;
		var upDir = Coord.Up;
		return new State
		{
			Id = instance.Id,
			Position = position,
			ForwardDirection = fwd,
			UpDirection = upDir,
			RightDirection = Coord.Cross(upDir, fwd),
			ActionPoints = stats.MaxAp,
			Stats = stats,
		};
	}
}
