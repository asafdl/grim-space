using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public interface IActorActionDef
{
	IAction Bind(string actorId);
}

public interface IMountedActionDef
{
	bool SupportsMount(ESpatialOrientation mountedOn);
	IAction Bind(string actorId, ESpatialOrientation mountedOn);
}

public interface IMountedAction : IAction
{
	ESpatialOrientation MountedOn { get; }
}
