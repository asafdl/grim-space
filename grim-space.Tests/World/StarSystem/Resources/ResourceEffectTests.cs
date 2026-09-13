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

		new ChangeResourceEffect(ResourceBundle.Of(ResourceId.Credits, 100)).Apply(map, runtime, "actor");

		Assert.Equal(100, map.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void ChangeResourceEffect_InsufficientFunds_DoesNotMutate()
	{
		var map = maps.Fresh(7);
		var runtime = new ActorRuntime();

		new ChangeResourceEffect(ResourceBundle.Of(ResourceId.Credits, -10)).Apply(map, runtime, "actor");

		Assert.Equal(0, map.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void Fork_PreservesBalancesAndMutatesIndependently()
	{
		var map = maps.Fresh(42);
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(ResourceBundle.Create(
			(ResourceId.Credits, 75),
			(ResourceId.ScrapAlloy, 10))).Apply(map, runtime, "actor");

		var fork = map.Fork();

		Assert.NotSame(map.PlayerResources, fork.PlayerResources);
		Assert.Equal(75, fork.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(10, fork.PlayerResources.GetBalance(ResourceId.ScrapAlloy));

		new ChangeResourceEffect(ResourceBundle.Of(ResourceId.Credits, 25))
			.Apply(fork, runtime, "actor");

		Assert.Equal(75, map.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(100, fork.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void ChangeResourceEffect_Undo_RestoresSnapshot()
	{
		var map = maps.Fresh(7);
		var runtime = new ActorRuntime();
		var effect = new ChangeResourceEffect(ResourceBundle.Of(ResourceId.IndustrialCore, 5));

		effect.Apply(map, runtime, "actor");
		Assert.Equal(5, map.PlayerResources.GetBalance(ResourceId.IndustrialCore));

		effect.Undo(map, runtime, "actor");
		Assert.Equal(0, map.PlayerResources.GetBalance(ResourceId.IndustrialCore));
	}
}
