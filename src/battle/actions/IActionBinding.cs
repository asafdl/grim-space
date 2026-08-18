using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public interface IActorActionDef
{
	IAction Bind(string actorId);
}

public interface IMountedActionDef
{
	IAction Bind(string actorId, ESpatialOrientation mountedOn);
}
