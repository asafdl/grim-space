using GrimSpace.Core.Engine;

namespace GrimSpace.Tests.Engine;

public sealed class ActorRuntimesTests
{
	[Fact]
	public void RemoveResetsAndRemovesRuntime()
	{
		var runtimes = new ActorRuntimes<TestRuntime>();
		var removed = runtimes.For("actor");
		removed.Value = 42;

		Assert.True(runtimes.Remove("actor"));
		Assert.Equal(0, removed.Value);

		var replacement = runtimes.For("actor");
		Assert.NotSame(removed, replacement);
		Assert.Equal(0, replacement.Value);
	}

	[Fact]
	public void RemoveMissingActorReturnsFalse()
	{
		var runtimes = new ActorRuntimes<TestRuntime>();

		Assert.False(runtimes.Remove("missing"));
	}

	private sealed class TestRuntime : IRuntimeContext<TestRuntime>
	{
		public int Value { get; set; }

		public TestRuntime Fork() => new() { Value = Value };

		public void Reset() => Value = 0;
	}
}
