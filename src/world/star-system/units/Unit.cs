namespace GrimSpace.World.StarSystem.Units;

public sealed class Unit
{
	public State State { get; }

	public Unit(State state) =>
		State = state;
}
