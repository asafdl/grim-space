using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units.Enums;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Units;

public sealed class Enemy : Unit
{
	public Enemy(State state, IMovement movement, IActions actions)
		: base(EController.Enemy, state, movement, actions)
	{
	}

	public override Preview? ShowMovement(BattleGrid grid) => null;
}
