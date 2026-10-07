using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public interface IAreaActionDef
{
	bool IsBlockable { get; }

	IReadOnlySet<Coord> AffectedCells(IAction action, BattleWorld world);
}
