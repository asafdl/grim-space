using GrimSpace.Battle.Movement;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Runtime;

public sealed class ActorSession : IRuntimeContext<ActorSession>
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

	public void Reset()
	{
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

	public ActorSession Fork() => ActorSessionCopy.Clone(this);
}

public readonly record struct ActorSessionSnapshot(
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

public static class ActorSessionCopy
{
	public static ActorSessionSnapshot Snapshot(ActorSession session) =>
		new(
			session.RawYawQuarters,
			session.MomentumPaid,
			session.MomentumGainedFromMovement,
			session.SpinBraked,
			session.SpinDiscount,
			session.MinPathApCost,
			session.PathApSpent,
			session.PathForwardSteps,
			session.UsedDirectionsMask,
			session.MoveStartMomentumLevel,
			session.MovementBuildupLevel,
			session.MovementBuildupForwardSteps,
			session.FlakUsedThisTurn);

	public static void Restore(ActorSession session, ActorSessionSnapshot snapshot)
	{
		session.RawYawQuarters = snapshot.RawYawQuarters;
		session.MomentumPaid = snapshot.MomentumPaid;
		session.MomentumGainedFromMovement = snapshot.MomentumGainedFromMovement;
		session.SpinBraked = snapshot.SpinBraked;
		session.SpinDiscount = snapshot.SpinDiscount;
		session.MinPathApCost = snapshot.MinPathApCost;
		session.PathApSpent = snapshot.PathApSpent;
		session.PathForwardSteps = snapshot.PathForwardSteps;
		session.UsedDirectionsMask = snapshot.UsedDirectionsMask;
		session.MoveStartMomentumLevel = snapshot.MoveStartMomentumLevel;
		session.MovementBuildupLevel = snapshot.MovementBuildupLevel;
		session.MovementBuildupForwardSteps = snapshot.MovementBuildupForwardSteps;
		session.FlakUsedThisTurn = snapshot.FlakUsedThisTurn;
	}

	public static ActorSession Clone(ActorSession session)
	{
		var clone = new ActorSession();
		Restore(clone, Snapshot(session));
		return clone;
	}
}
