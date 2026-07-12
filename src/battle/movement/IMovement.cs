using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Movement;

public interface IMovement
{
	IReadOnlyList<Option> GetPreviews(State unit, BoundedGrid grid);
	bool CanMove(State unit, Option option);
	void ApplyMove(State unit, Option option);
}
