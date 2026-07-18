using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class Player : Unit
{
	public Player(State state)
		: base(EController.Player, state)
	{
	}
}
