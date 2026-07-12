using GrimSpace.Battle.Movement;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Units;

public sealed class Player : Unit
{
	public Player(State state, IMovement movement)
		: base(EController.Player, state, movement)
	{
	}

	public override Preview? ShowMovement(BoundedGrid grid) =>
		new() { Options = Movement.GetPreviews(State, grid) };
}
