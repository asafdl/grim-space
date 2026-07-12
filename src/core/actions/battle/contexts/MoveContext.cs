using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct MoveContext(State player, Unit playerUnit)
{
	public void Apply(Option option) => playerUnit.Movement.ApplyMove(player, option);
}
