using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Movement;

public interface IMovement
{
	IReadOnlyList<Option> GetPreviews(State unit, BattleGrid grid, IActions actions);
	bool CanMove(State unit, Option option);
	void ApplyMove(State unit, Option option);
}
