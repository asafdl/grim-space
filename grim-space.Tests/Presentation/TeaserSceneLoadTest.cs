using Xunit;
using Godot;
using GrimSpace.Presentation.Teaser;

namespace grim_space.Tests.Presentation;

public class TeaserSceneLoadTest
{
	// The teaser scene is a Godot scene: loading it needs a live Godot runtime.
	// AGENTS.md forbids exercising the Godot framework from this test project because
	// doing so aborts the whole test host with a native access violation (0xC0000005)
	// inside ResourceLoader's static constructor. The teaser scene is instead covered by
	// the recording pipeline (Godot Movie Maker), not by a unit test.
	[Fact(Skip = "Touches the Godot runtime; aborts the test host. Covered by the teaser recording instead.")]
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
