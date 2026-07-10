using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions;

public sealed record MoveAction(Option Option) : IAction;
