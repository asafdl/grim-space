using GrimSpace.Battle.Movement;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Runtime;

public sealed class ActorRuntime : IRuntimeContext<ActorRuntime>
{
	public int RawYawQuarters { get; set; }
	public int MomentumPaid { get; set; }
	public int MomentumGainedFromMovement { get; set; }
	public bool SpinBraked { get; set; }
	public bool SpinDiscount { get; set; }

	public int NetYaw => Orientation.NormalizeQuarters(RawYawQuarters);

	public void Reset()
	{
		RawYawQuarters = 0;
		MomentumPaid = 0;
		MomentumGainedFromMovement = 0;
		SpinBraked = false;
		SpinDiscount = false;
	}

	public ActorRuntime Fork() => ActorRuntimeCopy.Clone(this);
}

public readonly record struct ActorRuntimeSnapshot(
	int RawYawQuarters,
	int MomentumPaid,
	int MomentumGainedFromMovement,
	bool SpinBraked,
	bool SpinDiscount);

public static class ActorRuntimeCopy
{
	public static ActorRuntimeSnapshot Snapshot(ActorRuntime session) =>
		new(
			session.RawYawQuarters,
			session.MomentumPaid,
			session.MomentumGainedFromMovement,
			session.SpinBraked,
			session.SpinDiscount);

	public static void Restore(ActorRuntime session, ActorRuntimeSnapshot snapshot)
	{
		session.RawYawQuarters = snapshot.RawYawQuarters;
		session.MomentumPaid = snapshot.MomentumPaid;
		session.MomentumGainedFromMovement = snapshot.MomentumGainedFromMovement;
		session.SpinBraked = snapshot.SpinBraked;
		session.SpinDiscount = snapshot.SpinDiscount;
	}

	public static ActorRuntime Clone(ActorRuntime session)
	{
		var clone = new ActorRuntime();
		Restore(clone, Snapshot(session));
		return clone;
	}
}
