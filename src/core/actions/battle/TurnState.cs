using GrimSpace.Battle.Movement;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Temporary per-turn planning state. Recreated on replay; never survives commit.
/// </summary>
public sealed class TurnState
{
	private int _rawYawQuarters;
	private int _momentumPaid;
	private bool _spinBraked;
	private bool _spinDiscount;

	public int RawYawQuarters => _rawYawQuarters;

	public int NetYaw => Orientation.NormalizeQuarters(_rawYawQuarters);

	public int MomentumPaidThisTurn => _momentumPaid;

	public bool SpinBraked => _spinBraked;

	public bool HasSpinDiscount => _spinDiscount;

	public void AddYawQuarters(int delta) => _rawYawQuarters += delta;

	public void AddMomentumPaid(int amount)
	{
		if (amount > 0)
			_momentumPaid += amount;
	}

	public int RefundMomentum(int requested)
	{
		var refund = System.Math.Min(requested, _momentumPaid);
		_momentumPaid -= refund;
		return refund;
	}

	public void MarkBrakedFromRetro()
	{
		_spinBraked = true;
		_spinDiscount = true;
	}

	public bool TryConsumeSpinDiscount()
	{
		if (!_spinDiscount)
			return false;

		_spinDiscount = false;
		return true;
	}

	public void Clear()
	{
		_rawYawQuarters = 0;
		_momentumPaid = 0;
		_spinBraked = false;
		_spinDiscount = false;
	}
}
