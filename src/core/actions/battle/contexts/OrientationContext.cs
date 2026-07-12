using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct OrientationContext(State player)
{
	public void Roll(ERollDirection direction) => Orientation.ApplyRoll(player, direction);

	public void Turn(EHeadingTurn turn) => Orientation.ApplyHeadingTurn(player, turn);
}
