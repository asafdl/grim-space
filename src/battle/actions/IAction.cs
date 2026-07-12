using GrimSpace.Battle.Actions.Effects;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions;

public interface IAction
{
	int GetApCost(State player);
	IReadOnlyList<IStateEffect> Resolve(ActionBoard board);
}
