using GrimSpace.Core.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.Tests.World.StarSystem.Resources;

public sealed class ResourceEffectTests(StarMapFixture maps)
{
	[Fact]
	public void ChangeResourceEffect_AppliesSignedDelta()
	{
		var map = maps.Fresh(7);
		var runtime = new ActorRuntime();

		new ChangeResourceEffect(TransactionSource.BattleLoot, ResourceBundle.Of(ResourceId.Credits, 100))
			.Apply(map, runtime, "actor");

		Assert.Equal(100, map.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void ChangeResourceEffect_InsufficientFunds_DoesNotMutate()
	{
		var map = maps.Fresh(7);
		var runtime = new ActorRuntime();

		new ChangeResourceEffect(TransactionSource.BattleLoot, ResourceBundle.Of(ResourceId.Credits, -10))
			.Apply(map, runtime, "actor");

		Assert.Equal(0, map.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void Fork_PreservesBalancesAndMutatesIndependently()
	{
		var map = maps.Fresh(42);
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(TransactionSource.BattleLoot, ResourceBundle.Create(
			(ResourceId.Credits, 75),
			(ResourceId.ScrapAlloy, 10))).Apply(map, runtime, "actor");

		var fork = map.Fork();

		Assert.NotSame(map.PlayerResources, fork.PlayerResources);
		Assert.Equal(75, fork.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(10, fork.PlayerResources.GetBalance(ResourceId.ScrapAlloy));

		new ChangeResourceEffect(TransactionSource.BattleLoot, ResourceBundle.Of(ResourceId.Credits, 25))
			.Apply(fork, runtime, "actor");

		Assert.Equal(75, map.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(100, fork.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void ChangeResourceEffect_Undo_RestoresSnapshot()
	{
		var map = maps.Fresh(7);
		var runtime = new ActorRuntime();
		var effect = new ChangeResourceEffect(TransactionSource.BattleLoot, ResourceBundle.Of(ResourceId.IndustrialCore, 5));

		effect.Apply(map, runtime, "actor");
		Assert.Equal(5, map.PlayerResources.GetBalance(ResourceId.IndustrialCore));

		effect.Undo(map, runtime, "actor");
		Assert.Equal(0, map.PlayerResources.GetBalance(ResourceId.IndustrialCore));
	}

	[Fact]
	public void ChangeResourceEffect_AppendsTransactionRecord()
	{
		var map = maps.Fresh(7);
		var runtime = new ActorRuntime();
		var change = ResourceBundle.Of(ResourceId.ScrapAlloy, 80);

		var records = new ChangeResourceEffect(TransactionSource.BattleLoot, change)
			.Apply(map, runtime, "actor");

		var record = Assert.IsType<Record<Transaction>>(Assert.Single(records));
		Assert.Equal(TransactionSource.BattleLoot, record.Value.Source);
		Assert.True(record.Value.Change.TryGet(ResourceId.ScrapAlloy, out var amount));
		Assert.Equal(80, amount);
	}

	[Fact]
	public void ChangeResourceEffect_EmptyChange_ReturnsNoRecords()
	{
		var map = maps.Fresh(7);
		var runtime = new ActorRuntime();

		var records = new ChangeResourceEffect(TransactionSource.BattleLoot, ResourceBundle.Empty)
			.Apply(map, runtime, "actor");

		Assert.Empty(records);
	}
}
