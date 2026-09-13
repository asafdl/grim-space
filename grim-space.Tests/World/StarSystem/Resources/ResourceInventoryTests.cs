using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Resources;

public sealed class ResourceInventoryTests
{
	[Fact]
	public void DefaultBalances_AreZero()
	{
		var inventory = new ResourceInventory();

		foreach (var id in Enum.GetValues<ResourceId>())
			Assert.Equal(0, inventory.GetBalance(id));
	}

	[Fact]
	public void TryApply_PositiveDelta_UpdatesEveryResource()
	{
		var inventory = new ResourceInventory();
		var change = ResourceBundle.Create(
			(ResourceId.Credits, 100),
			(ResourceId.ScrapAlloy, 25),
			(ResourceId.IndustrialCore, 3));

		Assert.True(inventory.TryApply(change));
		Assert.Equal(100, inventory.GetBalance(ResourceId.Credits));
		Assert.Equal(25, inventory.GetBalance(ResourceId.ScrapAlloy));
		Assert.Equal(3, inventory.GetBalance(ResourceId.IndustrialCore));
	}

	[Fact]
	public void Create_DropsZeroEntries()
	{
		var bundle = ResourceBundle.Create(
			(ResourceId.Credits, 0),
			(ResourceId.ScrapAlloy, 5),
			(ResourceId.IndustrialCore, 0));

		Assert.False(bundle.IsEmpty);
		Assert.False(bundle.TryGet(ResourceId.Credits, out _));
		Assert.True(bundle.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(5, scrap);
		Assert.False(bundle.TryGet(ResourceId.IndustrialCore, out _));
	}

	[Fact]
	public void TryApply_UnderflowRejected_NoPartialMutation()
	{
		var inventory = new ResourceInventory();
		Assert.True(inventory.TryApply(ResourceBundle.Of(ResourceId.Credits, 10)));

		var change = ResourceBundle.Create(
			(ResourceId.Credits, -6),
			(ResourceId.ScrapAlloy, -1));

		Assert.False(inventory.TryApply(change));
		Assert.Equal(10, inventory.GetBalance(ResourceId.Credits));
		Assert.Equal(0, inventory.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void TryApply_OverflowRejected_NoPartialMutation()
	{
		var inventory = new ResourceInventory();
		Assert.True(inventory.TryApply(ResourceBundle.Of(ResourceId.Credits, int.MaxValue)));

		Assert.False(inventory.TryApply(ResourceBundle.Of(ResourceId.Credits, 1)));
		Assert.Equal(int.MaxValue, inventory.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void CanApply_MatchesTryApplyWithoutMutating()
	{
		var inventory = new ResourceInventory();
		Assert.True(inventory.TryApply(ResourceBundle.Of(ResourceId.Credits, 40)));

		var affordable = ResourceBundle.Of(ResourceId.Credits, -20);
		Assert.True(inventory.CanApply(affordable));
		Assert.Equal(40, inventory.GetBalance(ResourceId.Credits));

		var unaffordable = ResourceBundle.Of(ResourceId.Credits, -50);
		Assert.False(inventory.CanApply(unaffordable));
		Assert.Equal(40, inventory.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void TryApply_FailedDebit_LeavesBalancesUnchanged()
	{
		var inventory = new ResourceInventory();
		Assert.True(inventory.TryApply(ResourceBundle.Of(ResourceId.ScrapAlloy, 12)));

		Assert.False(inventory.TryApply(ResourceBundle.Of(ResourceId.ScrapAlloy, -20)));
		Assert.Equal(12, inventory.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void TryApply_SuccessfulDebit_DeductsAtomically()
	{
		var inventory = new ResourceInventory();
		Assert.True(inventory.TryApply(ResourceBundle.Create(
			(ResourceId.Credits, 50),
			(ResourceId.IndustrialCore, 4))));

		Assert.True(inventory.TryApply(ResourceBundle.Create(
			(ResourceId.Credits, -30),
			(ResourceId.IndustrialCore, -2))));

		Assert.Equal(20, inventory.GetBalance(ResourceId.Credits));
		Assert.Equal(2, inventory.GetBalance(ResourceId.IndustrialCore));
	}

}
