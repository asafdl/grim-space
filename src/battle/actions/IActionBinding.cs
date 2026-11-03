using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public interface IActorActionDef
{
	IAction Bind(string actorId);
}

public interface IOrientedActionDef
{
	IAction Bind(string actorId, ESpatialOrientation orientation);
}

public interface ITargetedActionDef
{
	IAction Bind(string actorId, IReadOnlyList<string> pickedIds);
}

public interface IOrientedAction : IAction
{
	ESpatialOrientation Orientation { get; }
}

public interface ITargetedAction : IAction
{
	string TargetId { get; }
}
