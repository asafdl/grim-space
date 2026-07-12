using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions.Contexts;

public readonly struct MoveContext(State player, Unit playerUnit)
{
	public void Apply(Option option) => playerUnit.Movement.ApplyMove(player, option);
}
