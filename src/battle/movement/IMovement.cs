using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Units;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Movement;

public interface IMovement
{
	IReadOnlyList<Option> GetPreviews(State unit, BattleGrid grid);
	bool CanMove(State unit, Option option);
	void ApplyMove(State unit, Option option);
}
