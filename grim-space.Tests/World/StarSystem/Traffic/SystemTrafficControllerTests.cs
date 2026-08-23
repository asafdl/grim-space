using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class SystemTrafficControllerTests
{
	[Fact]
	public void TryRegisterLane_AllowsMultipleActorsOnSameRoute()
	{
		var controller = new SystemTrafficController();

		Assert.True(controller.TryRegisterLane("actor-a", "route:test", towardDockB: true));
		Assert.True(controller.TryRegisterLane("actor-b", "route:test", towardDockB: false));

		Assert.Equal(2, controller.OccupantsByRouteId["route:test"].Count);
		Assert.Contains("actor-a", controller.OccupantsByRouteId["route:test"]);
		Assert.Contains("actor-b", controller.OccupantsByRouteId["route:test"]);
		controller.Validate();
	}

	[Fact]
	public void TryRegisterLane_RejectsDuplicateActorRegistration()
	{
		var controller = new SystemTrafficController();
		controller.TryRegisterLane("actor-a", "route:test", towardDockB: true);

		Assert.False(controller.TryRegisterLane("actor-a", "route:other", towardDockB: true));
	}

	[Fact]
	public void UnregisterLane_RemovesActorFromOccupancyIndex()
	{
		var controller = new SystemTrafficController();
		controller.TryRegisterLane("actor-a", "route:test", towardDockB: true);
		controller.TryRegisterLane("actor-b", "route:test", towardDockB: true);

		controller.UnregisterLane("actor-a");

		Assert.Single(controller.OccupantsByRouteId["route:test"]);
		Assert.Contains("actor-b", controller.OccupantsByRouteId["route:test"]);
		controller.Validate();
	}

	[Fact]
	public void VerifyLane_AlwaysApprovesForNow()
	{
		var controller = new SystemTrafficController();

		Assert.True(controller.VerifyLane("actor-a", "route:test", towardDockB: true));
	}

	[Fact]
	public void Fork_ClonesOccupancyIndependently()
	{
		var controller = new SystemTrafficController();
		controller.TryRegisterLane("actor-a", "route:test", towardDockB: true);

		var fork = controller.Fork();
		fork.UnregisterLane("actor-a");

		Assert.Single(controller.OccupantsByRouteId["route:test"]);
		Assert.Empty(fork.OccupantsByRouteId);
	}
}
