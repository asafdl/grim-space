using GrimSpace.Application;
using Godot;

namespace GrimSpace.Tests.Application;

public sealed class GameInputBindingsValidationTests
{
	[Fact]
	public void TryValidateCommitted_accepts_default_bindings()
	{
		var defaults = GameInputBindings.DefaultBindings();
		Assert.True(GameInputBindings.TryValidateCommitted(defaults, out var error));
		Assert.Null(error);
	}

	[Fact]
	public void TryValidateCommitted_rejects_missing_primary()
	{
		var bindings = new Dictionary<StringName, BindingPair>(GameInputBindings.DefaultBindings());
		var action = GameInputBindings.Specs[0].Action;
		bindings[action] = new BindingPair(KeyBinding.Empty, null);

		Assert.False(GameInputBindings.TryValidateCommitted(bindings, out var error));
		Assert.Contains("primary binding", error, StringComparison.Ordinal);
	}
}
