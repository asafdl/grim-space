using Xunit;
using Godot;
using GrimSpace.Presentation.Teaser;

namespace grim_space.Tests.Presentation;

public class TeaserSceneLoadTest
{
	[Fact]
	public void TeaserScene_LoadsWithoutError()
	{
		var scene = GD.Load<PackedScene>("res://scenes/teaser.tscn");
		Assert.NotNull(scene);

		var instance = scene.Instantiate<TeaserController>();
		Assert.NotNull(instance);
		Assert.IsType<TeaserController>(instance);

		instance.QueueFree();
	}
}