using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Domain.Units.Enums;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Units;

public sealed class Player : Unit
{
	public Player(State state, IMovement movement, IActions actions)
		: base(EController.Player, state, movement, actions)
	{
	}

	public override Preview? ShowMovement(BattleGrid grid) =>
		new() { Options = Movement.GetPreviews(State, grid, Actions) };
}
