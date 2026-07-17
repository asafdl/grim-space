using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Actions.Battle;

public sealed class SpinState
{
	private const string Braked = "braked";
	private const string Discount = "discount";

	private readonly TagCharges _charges = new();

	public bool IsBraked => _charges.Get(Braked) > 0;

	public bool HasDiscount => _charges.Get(Discount) > 0;

	public void MarkBrakedFromRetro()
	{
		_charges.Set(Braked, 1);
		_charges.Set(Discount, 1);
	}

	public bool TryConsumeDiscount() => _charges.TryConsume(Discount, 1);

	public void Clear() => _charges.Clear();
}
