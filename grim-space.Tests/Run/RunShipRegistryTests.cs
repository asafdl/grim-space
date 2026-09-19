using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Tests.Run;

public sealed class RunShipRegistryTests
{
	[Fact]
	public void Register_IsIdempotentForMatchingConfiguration()
	{
		var registry = new RunShipRegistry();
		var ship = RunShip.CreateDefault("fighter-a", EType.Fighter);

		registry.Register(ship);
		registry.Register(RunShip.CreateDefault("fighter-a", EType.Fighter));

		Assert.Single(registry.All);
	}

	[Fact]
	public void Register_ThrowsWhenConfigurationConflicts()
	{
		var registry = new RunShipRegistry();
		registry.Register(RunShip.CreateDefault("fighter-a", EType.Fighter));

		Assert.Throws<InvalidOperationException>(() =>
			registry.Register(RunShip.CreateDefault("fighter-a", EType.Patrol)));
	}

	[Fact]
	public void Snapshot_ReflectsRegistryEditsBeforeEnlistment()
	{
		var registry = new RunShipRegistry();
		registry.Register(RunShip.CreateDefault("fighter-a", EType.Fighter));
		registry.Get("fighter-a").HullPoints = 1;

		var snapshot = registry.Snapshot("fighter-a");

		Assert.Equal(1, snapshot.HullPoints);
	}

	[Fact]
	public void ApplyHandoff_UpdatesVitals()
	{
		var registry = new RunShipRegistry();
		registry.Register(RunShip.CreateDefault("patrol-a", EType.Patrol));
		var shields = FaceShieldPoints.MaxFor(EType.Patrol);
		shields[GrimSpace.Math.Grid.ESpatialOrientation.Forward] = 1;

		registry.ApplyHandoff(new GrimSpace.Battle.Objectives.UnitStateHandoff(
			"patrol-a",
			EType.Patrol,
			0,
			shields));

		Assert.Equal(0, registry.Get("patrol-a").HullPoints);
		Assert.Equal(1, registry.Get("patrol-a").ShieldPoints[GrimSpace.Math.Grid.ESpatialOrientation.Forward]);
	}
}
