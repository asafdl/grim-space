namespace GrimSpace.World.StarSystem.Resources;

public sealed class ResourceInventory
{
	private readonly Dictionary<ResourceId, int> _balances = new();

	public int GetBalance(ResourceId id) => _balances.GetValueOrDefault(id);

	public IEnumerable<(ResourceId Id, int Balance)> EnumerateBalances()
	{
		foreach (var id in Enum.GetValues<ResourceId>())
			yield return (id, GetBalance(id));
	}

	public bool CanApply(ResourceBundle change) => TryValidate(change, out _);

	public bool TryApply(ResourceBundle change)
	{
		if (!TryValidate(change, out var nextBalances))
			return false;

		_balances.Clear();
		foreach (var (id, balance) in nextBalances!)
			_balances[id] = balance;

		return true;
	}

	internal ResourceInventory Clone()
	{
		var clone = new ResourceInventory();
		foreach (var (id, balance) in _balances)
			clone._balances[id] = balance;

		return clone;
	}

	private bool TryValidate(ResourceBundle change, out Dictionary<ResourceId, int>? nextBalances)
	{
		nextBalances = null;
		if (change.IsEmpty)
			return true;

		var scratch = new Dictionary<ResourceId, int>(_balances);
		foreach (var (id, delta) in change)
		{
			var current = scratch.GetValueOrDefault(id);
			int next;
			try
			{
				next = checked(current + delta);
			}
			catch (OverflowException)
			{
				return false;
			}

			if (next < 0)
				return false;

			if (next == 0)
				scratch.Remove(id);
			else
				scratch[id] = next;
		}

		nextBalances = scratch;
		return true;
	}
}
