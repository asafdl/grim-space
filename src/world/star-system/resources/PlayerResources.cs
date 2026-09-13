namespace GrimSpace.World.StarSystem.Resources;

public sealed class PlayerResources
{
	private ResourceInventory _inventory = new();

	public int GetBalance(ResourceId id) => _inventory.GetBalance(id);

	public IEnumerable<(ResourceId Id, int Balance)> EnumerateBalances() =>
		_inventory.EnumerateBalances();

	internal bool CanApply(ResourceBundle change) => _inventory.CanApply(change);

	internal bool TryApply(ResourceBundle change) => _inventory.TryApply(change);

	internal ResourceInventory Snapshot() => _inventory.Clone();

	internal void Restore(ResourceInventory snapshot) => _inventory = snapshot.Clone();

	public PlayerResources CloneForFork() =>
		new() { _inventory = _inventory.Clone() };
}
