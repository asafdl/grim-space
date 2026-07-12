using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions.Contexts;

public readonly struct OrientationContext(State player)
{
	public void Roll(ERollDirection direction) => Orientation.ApplyRoll(player, direction);

	public void Turn(EHeadingTurn turn) => Orientation.ApplyHeadingTurn(player, turn);
}
