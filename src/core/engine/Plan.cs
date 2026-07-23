using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed class Plan(IReadOnlyList<IAction> actions)
{
	public IReadOnlyList<IAction> Actions { get; } = actions;
}
