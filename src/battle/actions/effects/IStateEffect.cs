using GrimSpace.Battle.Actions.Contexts;

namespace GrimSpace.Battle.Actions.Effects;

public interface IStateEffect
{
	void Apply(ActionSlices slices);
}
