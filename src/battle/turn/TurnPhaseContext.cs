using GrimSpace.Battle.Movement;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Turn;

public sealed class TurnPhaseContext : IRuntimeContext<TurnPhaseContext>
{
	public const int InitialMinPathApCost = 3;

	public int RawYawQuarters { get; set; }
	public int MomentumPaid { get; set; }
	public int MomentumGainedFromMovement { get; set; }
	public bool SpinBraked { get; set; }
	public bool SpinDiscount { get; set; }
	public int MinPathApCost { get; set; } = InitialMinPathApCost;
	public int PathApSpent { get; set; }
	public int PathForwardSteps { get; set; }
	public int UsedDirectionsMask { get; set; }
	public int MoveStartMomentumLevel { get; set; }
	public int MovementBuildupLevel { get; set; }
	public int MovementBuildupForwardSteps { get; set; }
	public bool FlakUsedThisTurn { get; set; }

	public int NetYaw => Orientation.NormalizeQuarters(RawYawQuarters);

	public bool IsMovePathStarted => PathForwardSteps > 0 || UsedDirectionsMask > 0;

	public MomentumConfig.Buildup MovementBuildup =>
		new(MovementBuildupLevel, MovementBuildupForwardSteps);

	public void Reset() {
		RawYawQuarters = 0;
		MomentumPaid = 0;
		MomentumGainedFromMovement = 0;
		SpinBraked = false;
		SpinDiscount = false;
		MinPathApCost = InitialMinPathApCost;
		PathApSpent = 0;
		PathForwardSteps = 0;
		UsedDirectionsMask = 0;
		MoveStartMomentumLevel = 0;
		MovementBuildupLevel = 0;
		MovementBuildupForwardSteps = 0;
		FlakUsedThisTurn = false;
	}

	public TurnPhaseContext Fork() => TurnPhaseContextCopy.Clone(this);
}

public readonly record struct TurnPhaseContextSnapshot(
	int RawYawQuarters,
	int MomentumPaid,
	int MomentumGainedFromMovement,
	bool SpinBraked,
	bool SpinDiscount,
	int MinPathApCost,
	int PathApSpent,
	int PathForwardSteps,
	int UsedDirectionsMask,
	int MoveStartMomentumLevel,
	int MovementBuildupLevel,
	int MovementBuildupForwardSteps,
	bool FlakUsedThisTurn);

public static class TurnPhaseContextCopy
{
	public static TurnPhaseContextSnapshot Snapshot(TurnPhaseContext context) =>
		new(
			context.RawYawQuarters,
			context.MomentumPaid,
			context.MomentumGainedFromMovement,
			context.SpinBraked,
			context.SpinDiscount,
			context.MinPathApCost,
			context.PathApSpent,
			context.PathForwardSteps,
			context.UsedDirectionsMask,
			context.MoveStartMomentumLevel,
			context.MovementBuildupLevel,
			context.MovementBuildupForwardSteps,
			context.FlakUsedThisTurn);

	public static void Restore(TurnPhaseContext context, TurnPhaseContextSnapshot snapshot)
	{
		context.RawYawQuarters = snapshot.RawYawQuarters;
		context.MomentumPaid = snapshot.MomentumPaid;
		context.MomentumGainedFromMovement = snapshot.MomentumGainedFromMovement;
		context.SpinBraked = snapshot.SpinBraked;
		context.SpinDiscount = snapshot.SpinDiscount;
		context.MinPathApCost = snapshot.MinPathApCost;
		context.PathApSpent = snapshot.PathApSpent;
		context.PathForwardSteps = snapshot.PathForwardSteps;
		context.UsedDirectionsMask = snapshot.UsedDirectionsMask;
		context.MoveStartMomentumLevel = snapshot.MoveStartMomentumLevel;
		context.MovementBuildupLevel = snapshot.MovementBuildupLevel;
		context.MovementBuildupForwardSteps = snapshot.MovementBuildupForwardSteps;
		context.FlakUsedThisTurn = snapshot.FlakUsedThisTurn;
	}

	public static TurnPhaseContext Clone(TurnPhaseContext context)
	{
		var clone = new TurnPhaseContext();
		Restore(clone, Snapshot(context));
		return clone;
	}
}
