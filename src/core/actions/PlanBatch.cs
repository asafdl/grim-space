namespace GrimSpace.Core.Actions;

public readonly record struct PlanBatch(IReadOnlyList<IAction> Actions);
