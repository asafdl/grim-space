using GrimSpace.Battle.Movement;
using GrimSpace.Domain.Combat;
using GrimSpace.Domain.Grid;

namespace GrimSpace.Battle.Planning;

public abstract record PlannedAction;

public sealed record PlannedMove(Option Option) : PlannedAction;

public sealed record PlannedMissile(Coord Center, EMissileMount Mount) : PlannedAction;

public sealed record PlannedRailgun(string TargetUnitId) : PlannedAction;
