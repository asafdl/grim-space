using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed class TickResult(int tick, IReadOnlyList<IAction> appliedActions)
{
	public int Tick { get; } = tick;

	public IReadOnlyList<IAction> AppliedActions { get; } = appliedActions;
}
