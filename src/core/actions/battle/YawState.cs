using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Actions.Battle;

public sealed class YawState
{
	private const string Quarters = "quarters";
	private const string MomentumPaid = "momentum_paid";

	private readonly TagCharges _charges = new();

	public int RawQuarters => _charges.Get(Quarters);

	public int NetQuarters => Orientation.NormalizeQuarters(RawQuarters);

	public int MomentumPaidThisTurn => _charges.Get(MomentumPaid);

	public void AddQuarters(int delta) => _charges.Add(Quarters, delta);

	public void AddMomentumPaid(int amount)
	{
		if (amount > 0)
			_charges.Add(MomentumPaid, amount);
	}

	public int RefundMomentum(int requested)
	{
		var refund = System.Math.Min(requested, MomentumPaidThisTurn);
		if (refund > 0)
			_charges.Add(MomentumPaid, -refund);

		return refund;
	}

	public void Clear() => _charges.Clear();
}
