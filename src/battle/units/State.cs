using GrimSpace.Battle.Grid;

namespace GrimSpace.Battle.Units;

public sealed class State
{
	public required string Id { get; init; }
	public Coord Position { get; set; }
	public Coord Velocity { get; set; }
	public Coord ForwardDirection { get; set; }
	public Coord UpDirection { get; set; }
	public Coord RightDirection { get; set; }
	public int ActionPoints { get; set; }
	public required Stats Stats { get; init; }

	public static State CreateDefault(
		string id,
		Coord position,
		Stats stats,
		Coord? forward = null,
		Coord? up = null)
	{
		var fwd = forward ?? Coord.Forward;
		var upDir = up ?? Coord.Up;
		return new State
		{
			Id = id,
			Position = position,
			Velocity = Coord.Zero,
			ForwardDirection = fwd,
			UpDirection = upDir,
			RightDirection = Coord.Cross(upDir, fwd),
			ActionPoints = stats.MaxAp,
			Stats = stats,
		};
	}

	public void RecalculateRightDirection() =>
		RightDirection = Coord.Cross(UpDirection, ForwardDirection);

	public State Clone() => new()
	{
		Id = Id,
		Position = Position,
		Velocity = Velocity,
		ForwardDirection = ForwardDirection,
		UpDirection = UpDirection,
		RightDirection = RightDirection,
		ActionPoints = ActionPoints,
		Stats = Stats,
	};
}
