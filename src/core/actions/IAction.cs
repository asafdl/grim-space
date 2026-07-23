using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public interface IAction
{
	string ActorId { get; }

	int? UndoGroup { get; }
}

public interface IAction<TWorld, TRuntime> : IAction
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
{
	IActionDef<IAction, TWorld, TRuntime, IEffect<TWorld, TRuntime>> Definition { get; }
}
