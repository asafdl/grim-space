using GrimSpace.Battle.Movement;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class Player : Unit
{
	public Player(State state, IMovement movement)
		: base(EController.Player, state, movement)
	{
	}
}
