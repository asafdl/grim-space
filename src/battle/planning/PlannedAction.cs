using GrimSpace.Battle.Movement;
using GrimSpace.Domain.Combat;
using GrimSpace.Domain.Grid;
using GrimSpace.Domain.Units.Enums;

namespace GrimSpace.Battle.Planning;

public abstract record PlannedAction;

public sealed record PlannedMove(Option Option) : PlannedAction;

public sealed record PlannedRoll(ERollDirection Direction) : PlannedAction;

public sealed record PlannedHeadingTurn(EHeadingTurn Turn) : PlannedAction;

public sealed record PlannedMissile(Coord Center, EMissileMount Mount) : PlannedAction;

public sealed record PlannedRailgun(string TargetUnitId) : PlannedAction;
