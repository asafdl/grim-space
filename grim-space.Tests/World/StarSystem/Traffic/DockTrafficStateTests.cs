using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class DockTrafficStateTests
{
	[Fact]
	public void TryAdmitArrival_OccupiesBerthWhenFree()
	{
		var dock = new DockTrafficState();
		var result = dock.TryAdmitArrival("actor-a");

		Assert.True(result.Admitted);
		Assert.False(result.Queued);
		dock.Validate();
	}

	[Fact]
	public void TryAdmitArrival_QueuesWhenBerthOccupied()
	{
		var dock = new DockTrafficState();
		dock.TryAdmitArrival("actor-a");
		var result = dock.TryAdmitArrival("actor-b");

		Assert.False(result.Admitted);
		Assert.True(result.Queued);
		dock.Validate();
	}

	[Fact]
	public void ReleaseArrival_WakesOutsideQueueHead()
	{
		var dock = new DockTrafficState();
		dock.TryAdmitArrival("actor-a");
		dock.TryAdmitArrival("actor-b");

		var release = dock.ReleaseArrival("actor-a");

		Assert.True(release.Released);
		Assert.Equal("actor-b", release.WokenActorId);
		dock.Validate();
	}

	[Fact]
	public void TryClaimDeparture_RequiresArrivalOccupancy()
	{
		var dock = new DockTrafficState();
		Assert.False(dock.TryClaimDeparture("actor-a"));

		dock.TryAdmitArrival("actor-a");
		Assert.True(dock.TryClaimDeparture("actor-a"));
		dock.Validate();
	}

	[Fact]
	public void Fork_ProducesIndependentCopy()
	{
		var dock = new DockTrafficState();
		dock.TryAdmitArrival("actor-a");

		var fork = dock.Fork();
		fork.TryAdmitArrival("actor-b");

		dock.Validate();
		fork.Validate();
		Assert.True(dock.TryAdmitArrival("actor-a").Admitted);
	}
}
