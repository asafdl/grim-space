using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class PoiTrafficRegistryTests
{
	private static PoiTrafficRegistry CreateRegistry() =>
		new(["segment-a", "segment-b"]);

	[Fact]
	public void TryAcquire_OccupiesFreeSegment()
	{
		var registry = CreateRegistry();
		var result = registry.TryAcquire("actor-a", "segment-a");

		Assert.True(result.Acquired);
		Assert.False(result.Waiting);
		registry.Validate();
	}

	[Fact]
	public void TryAcquire_WaitsWhenSegmentOccupied()
	{
		var registry = CreateRegistry();
		registry.TryAcquire("actor-a", "segment-a");
		var result = registry.TryAcquire("actor-b", "segment-a");

		Assert.False(result.Acquired);
		Assert.True(result.Waiting);
		registry.Validate();
	}

	[Fact]
	public void Release_WakesFifoHead()
	{
		var registry = CreateRegistry();
		registry.TryAcquire("actor-a", "segment-a");
		registry.TryAcquire("actor-b", "segment-a");
		registry.TryAcquire("actor-c", "segment-a");

		var release = registry.Release("actor-a", "segment-a");

		Assert.True(release.Released);
		Assert.Equal("actor-b", release.WokenActorId);
		registry.Validate();
	}

	[Fact]
	public void Transfer_AcquireNextBeforeReleaseCurrent()
	{
		var registry = CreateRegistry();
		registry.TryAcquire("actor-a", "segment-a");
		var acquire = registry.TryAcquire("actor-a", "segment-b");

		Assert.True(acquire.Acquired);
		registry.Release("actor-a", "segment-a");
		registry.Validate();
	}

	[Fact]
	public void Cancel_RemovesOwnershipAndQueueMembership()
	{
		var registry = CreateRegistry();
		registry.TryAcquire("actor-a", "segment-a");
		registry.TryAcquire("actor-b", "segment-a");
		registry.Cancel("actor-b");

		var release = registry.Release("actor-a", "segment-a");
		Assert.Null(release.WokenActorId);
		registry.Validate();
	}

	[Fact]
	public void Fork_ProducesIndependentCopy()
	{
		var registry = CreateRegistry();
		registry.TryAcquire("actor-a", "segment-a");

		var fork = registry.Fork();
		fork.TryAcquire("actor-b", "segment-b");

		registry.Validate();
		fork.Validate();
		Assert.True(registry.TryAcquire("actor-c", "segment-b").Acquired);
	}
}
